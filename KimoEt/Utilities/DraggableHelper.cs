using KimoEt.ProcessWindow;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KimoEt.Utililties
{
    public class DraggableHelper
    {
        private bool _isMoving;

        private Point? _buttonPosition;
        private double deltaX;
        private double deltaY;
        private TranslateTransform _currentTT;

        private UIElement element;
        private Canvas canvas;
        private MainWindow window;
        private IOnDragEnded onDragEndedListener;

        public interface IOnDragEnded
        {
            void OnDragEnded(UIElement element);
        }

        public DraggableHelper(UIElement element, Canvas canvas, MainWindow window, IOnDragEnded onDragEndedListener)
        {
            this.element = element;
            this.canvas = canvas;
            this.window = window;
            this.onDragEndedListener = onDragEndedListener;
        }

        public void Start()
        {
            window.PreviewMouseMove += Control_PreviewMouseMove;

            element.PreviewMouseDown += Control_PreviewMouseDown;
            element.PreviewMouseUp += Control_PreviewMouseUp;
            element.PreviewMouseMove += Control_PreviewMouseMove;

            //draggableComponents[element] = this;
        }

        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (window.SearchTextBox.IsFocused)
                return;

            if (_buttonPosition == null)
                _buttonPosition = element.TransformToAncestor(canvas).Transform(new Point(0, 0));
            var mousePosition = Mouse.GetPosition(canvas);
            deltaX = mousePosition.X - _buttonPosition.Value.X;
            deltaY = mousePosition.Y - _buttonPosition.Value.Y;

            ProcessWindowManager.Instance.ForceFocus();
            _isMoving = true;

        }

        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (window.SearchTextBox.IsFocused && !_isMoving)
                return;

            _currentTT = element.RenderTransform as TranslateTransform;
            ProcessWindowManager.Instance.ReleaseFocus();
            _isMoving = false;
            if (onDragEndedListener != null)
            {
                onDragEndedListener.OnDragEnded(element);
            }
        }

        private void Control_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMoving) return;

            var mousePoint = Mouse.GetPosition(canvas);

            var offsetX = (_currentTT == null ? _buttonPosition.Value.X : _buttonPosition.Value.X - _currentTT.X) + deltaX - mousePoint.X;
            var offsetY = (_currentTT == null ? _buttonPosition.Value.Y : _buttonPosition.Value.Y - _currentTT.Y) + deltaY - mousePoint.Y;

            element.RenderTransform = new TranslateTransform(-offsetX, -offsetY);
        }

    }
}
