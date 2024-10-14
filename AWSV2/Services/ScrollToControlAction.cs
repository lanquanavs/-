﻿using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace AWSV2.Services
{
    /// <summary>
    /// 在 ScrollViewer 中定位到指定的控件
    /// 说明：目前支持的是垂直滚动
    /// </summary>
    class ScrollToControlAction : TriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty ScrollViewerProperty =
          DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ScrollToControlAction), new PropertyMetadata(null));

        public static readonly DependencyProperty TargetControlProperty =
            DependencyProperty.Register("TargetControl", typeof(FrameworkElement), typeof(ScrollToControlAction), new PropertyMetadata(null));

        /// <summary>
        /// 目标 ScrollViewer
        /// </summary>
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        /// <summary>
        /// 要定位的到的控件
        /// </summary>
        public FrameworkElement TargetControl
        {
            get { return (FrameworkElement)GetValue(TargetControlProperty); }
            set { SetValue(TargetControlProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (TargetControl == null || ScrollViewer == null)
            {
                throw new ArgumentNullException($"{ScrollViewer} or {TargetControl} cannot be null");
            }

            
            // 获取要定位之前 ScrollViewer 目前的滚动位置
            var currentScrollPosition = ScrollViewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = TargetControl.TransformToVisual(ScrollViewer).Transform(point);
            ScrollViewer.ScrollToVerticalOffset(targetPosition.Y);
        }
    }
}
