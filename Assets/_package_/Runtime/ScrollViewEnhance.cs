using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIElementsKits
{
    /// <summary>
    /// ScrollView增强版
    /// 当ScrollView下有数量庞大的Item时,会导致性能急速下降造成卡顿
    /// 这个封装解决这个问题,通过只绘制窗口视图中的Item,其他看不到的地方用上下两个占位符填充实现性能提升
    /// </summary>
    public class ScrollViewEnhance
    {
        public ScrollView ScrollView { get; }

        private IResolvedStyle _irs => ScrollView;

        /// <summary>
        /// ScrollView中所有Item的数量
        /// </summary>
        public int ItemTotal { get; private set; }

        /// <summary>
        /// Item高度
        /// </summary>
        public float ItemHeight { get; private set; }

        /// <summary>
        /// 视图的高度
        /// </summary>
        public float ViewHeight
        {
            get
            {
                if (ScrollView.layout.height is float.NaN || ScrollView.layout.height == 0)
                {
                    return MaxViewHeight;
                }

                return ScrollView.layout.height;
            }
        }

        public float MaxViewHeight => _irs.maxHeight.value;

        /// <summary>
        /// 总高度 = ItemTotal * ItemHeight
        /// </summary>
        public float TotalHeight => ItemTotal * ItemHeight;

        /// <summary>
        /// 视图绘制的item数量(ViewHeight/ItemHeight)
        /// </summary>
        public int DrawCount => (int) (ViewHeight / ItemHeight);

        /// <summary>
        /// 顶部占位符
        /// </summary>
        private readonly VisualElement _topFilling;

        /// <summary>
        /// 底部占位符
        /// </summary>
        private readonly VisualElement _bottomFilling;

        private VisualElementPool _objectItemPool;

        private Func<int, VisualElement, object, bool> _drawItemAction;

        private readonly Dictionary<int /*index*/, VisualElement> _currentDisplayItem;

        private readonly List<int /*index*/> _recycleList;

        private float _lastOffset;

        private object _context;

        public ScrollViewEnhance(ScrollView scrollView)
        {
            ScrollView = scrollView;

            _topFilling = ScrollView.contentContainer.Q("top_filling_ve");
            if (_topFilling == null)
            {
                _topFilling = new VisualElement() {name = "top_filling_ve"};
                ScrollView.Add(_topFilling);
            }

            _bottomFilling = ScrollView.contentContainer.Q("bottom_filling_ve");
            if (_bottomFilling == null)
            {
                _bottomFilling = new VisualElement() {name = "bottom_filling_ve"};
                ScrollView.Add(_bottomFilling);
            }

            _currentDisplayItem = new Dictionary<int, VisualElement>();
            _recycleList = new List<int>();

            ScrollView.verticalScroller.valueChanged += Refresh;

            ScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // 当布局绘制完成后,重新计算item的高度,这时的高度值是准确的
            if (ScrollView.contentContainer.childCount <= 2)
            {
                return;
            }

            RefreshItemHeight(ScrollView.contentContainer[1][0]);
        }

        public ScrollViewEnhance SetItemCount(int value)
        {
            ItemTotal = value;
            return this;
        }

        public ScrollViewEnhance SetItemHeight(float value)
        {
            ItemHeight = value;
            return this;
        }

        public ScrollViewEnhance SetItemPool(VisualElementPool pool)
        {
            _objectItemPool = pool;

            // 当设置对象池后,会实例一个item读取item的高度用于视图布局
            var item = _objectItemPool.Get();
            IResolvedStyle irs = item[0];
            RefreshItemHeight(irs);
            _objectItemPool.Return(item);
            return this;
        }

        private void RefreshItemHeight(IResolvedStyle irs)
        {
            ItemHeight = (irs.height is float.NaN || irs.height == 0) ? irs.maxHeight.value : irs.height;
        }

        public ScrollViewEnhance SetDrawItemAction(Func<int, VisualElement, object, bool> func)
        {
            _drawItemAction = func;
            return this;
        }

        public void Redraw(int drawCount, object ctx = null)
        {
            _context = ctx;
            Clear();
            SetItemCount(drawCount);
            Refresh(0);
        }

        public void Refresh()
        {
            Refresh(_lastOffset);
        }

        public void Refresh(float offset)
        {
            var minIndex = (int) (offset / ItemHeight);

            var maxIndex = minIndex + DrawCount + 1;
            if (maxIndex >= ItemTotal)
            {
                maxIndex = ItemTotal - 1;
            }

            // 回收超出视图的item
            Recycle(offset, minIndex, maxIndex);

            // 绘制item
            var drawHeight = Draw(offset, minIndex, maxIndex, _context);

            // 刷新适应高度
            RefreshFillingHeight(offset, drawHeight);
        }

        public void Clear()
        {
            Recycle(0, 0, 0);
        }

        private float Draw(float offset, int minIndex, int maxIndex, object context)
        {
            float drawHeight = 0;

            // 滑块向下滑动,从尾部添加
            if (_lastOffset <= offset)
            {
                // 绘制新的item,item的index区间[minIndex,maxIndex]
                for (var index = minIndex; index <= maxIndex; index++)
                {
                    if (_drawItemAction == null)
                    {
                        Debug.LogError("DrawItemAction is null");
                        break;
                    }

                    if (_currentDisplayItem.TryGetValue(index, out var item))
                    {
                        continue;
                    }

                    item = _objectItemPool.Get();

                    var content = ScrollView.contentContainer;
                    var insertIndex = content.childCount - 1;

                    var ret = _drawItemAction.Invoke(index, item, context);
                    if (!ret)
                    {
                        _objectItemPool.Return(item);
                        continue;
                    }

                    content.Insert(insertIndex, item);
                    _currentDisplayItem.Add(index, item);
                }
            }
            // 滑块向上滑动,从头部添加
            else
            {
                // 绘制新的item,item的index区间[minIndex,maxIndex]
                for (var index = maxIndex; index >= minIndex; index--)
                {
                    if (_drawItemAction == null)
                    {
                        Debug.LogError("DrawItemAction is null");
                        break;
                    }

                    if (_currentDisplayItem.TryGetValue(index, out var item))
                    {
                        continue;
                    }

                    item = _objectItemPool.Get();

                    var ret = _drawItemAction.Invoke(index, item, context);

                    if (!ret)
                    {
                        _objectItemPool.Return(item);
                        continue;
                    }

                    ScrollView.contentContainer.Insert(1, item);
                    _currentDisplayItem.Add(index, item);
                }
            }

            _lastOffset = offset;

            foreach (var element in _currentDisplayItem)
            {
                if (element.Value.layout.height is float.NaN || element.Value.layout.height == 0)
                {
                    IResolvedStyle irs = element.Value;
                    drawHeight += irs.maxHeight.value;
                }
                else
                {
                    drawHeight += element.Value.layout.height;
                }
            }

            return drawHeight;
        }

        private void Recycle(float offset, int minIndex, int maxIndex)
        {
            _recycleList.Clear();

            foreach (var pair in _currentDisplayItem)
            {
                if (pair.Key < minIndex || pair.Key > maxIndex)
                {
                    _recycleList.Add(pair.Key);
                }
                else if (minIndex == maxIndex && pair.Key == maxIndex)
                {
                    _recycleList.Add(pair.Key);
                }
            }

            // 回收item进池子
            foreach (var index in _recycleList)
            {
                if (_currentDisplayItem.TryGetValue(index, out var item))
                {
                    _objectItemPool.Return(item);
                    ScrollView.contentContainer.Remove(item);
                    _currentDisplayItem.Remove(index);
                }
            }
        }

        private void RefreshFillingHeight(float offset, float drawHeight)
        {
            var mod = offset % ItemHeight;
            var th = offset - mod;
            _topFilling.style.height = new StyleLength(th);

            var bh = 0f;
            if (drawHeight < MaxViewHeight - ItemHeight)
            {
                bh = MaxViewHeight - drawHeight - 20;
                bh = 0;
            }
            else
            {
                bh = Mathf.Abs(TotalHeight - MaxViewHeight) - th;
            }

            bh = bh < 0 ? 0 : bh;
            _bottomFilling.style.height = new StyleLength(bh);
        }
    }
}