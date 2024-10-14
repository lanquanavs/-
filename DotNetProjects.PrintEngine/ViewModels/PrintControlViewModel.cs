using System;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using SUT.PrintEngine.Extensions;
using SUT.PrintEngine.Paginators;
using SUT.PrintEngine.Utils;
using SUT.PrintEngine.Views;

namespace SUT.PrintEngine.ViewModels
{
    public class PrintControlViewModel : APrintControlViewModel, IPrintControlViewModel
    {
        #region Commands

        public DrawingVisual DrawingVisual { get; set; }

        public ICommand ResizeCommand { get; set; }
        public ICommand ApplyScaleCommand { get; set; }
        public ICommand CancelScaleCommand { get; set; }
        #endregion

        #region Dependency Properties
        public double Scale
        {
            get
            {
                return (double)GetValue(ScaleProperty);
            }
            set
            {
                SetValue(ScaleProperty, value);
            }
        }

        //public static readonly DependencyProperty FitToPageProperty = DependencyProperty.Register("FitToPage", typeof(bool), typeof(PrintControlViewModel), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPropertyChanged)));

        //public bool FitToPage
        //{
        //    get { return (bool)GetValue(FitToPageProperty); }
        //    set { SetValue(FitToPageProperty, value); }
        //}

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            "Scale",
            typeof(double),
            typeof(PrintControlViewModel),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPropertyChanged)));

        private bool _isCancelPrint;

        #endregion

        public double ScaleFactor { get; set; }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PrintControlViewModel)d).HandlePropertyChanged(d, e);
        }

        public PrintControlViewModel(PrintControlView view)
            : base(view)
        {

            ResizeCommand = new DelegateCommand(ExecuteResize);
            ApplyScaleCommand = new DelegateCommand(ExecuteApplyScale);
            CancelScaleCommand = new DelegateCommand(ExecuteCancelScale);
            PrintControlView.ResizeButtonVisibility(true);
            PrintControlView.SetPageNumberVisibility(Visibility.Visible);


        }

        //private void FitToPage_Scale()
        //{
        //    if (FitToPage)
        //    {
        //        double NewScale = 0;
        //        int height, width;

        //        if (PageOrientation == System.Printing.PageOrientation.Landscape)
        //        {
        //            height = CurrentPaper.Width;
        //            width = CurrentPaper.Height;
        //        }
        //        else
        //        {
        //            height = CurrentPaper.Height;
        //            width = CurrentPaper.Width;
        //        }


        //        if (this.DrawingVisual.ContentBounds.Width > width || this.DrawingVisual.ContentBounds.Height > height)
        //        {
        //            double widthProportion = this.DrawingVisual.ContentBounds.Width / width;
        //            double heightProportion = this.DrawingVisual.ContentBounds.Height / height;

        //            if (widthProportion > heightProportion)
        //                NewScale = (width - 20.00) / this.DrawingVisual.ContentBounds.Width;
        //            else
        //                NewScale = (height - 20.00) / this.DrawingVisual.ContentBounds.Height;

        //            Scale = NewScale;
        //        }
        //    }
        //}

        public void ExecuteResize(object parameter)
        {
            PrintControlView.ScalePreviewPaneVisibility(true);
        }
        private void ExecuteCancelScale(object parameter)
        {
            ScaleCanceling = true;
            Scale = OldScale;
            PrintControlView.ScalePreviewPaneVisibility(false);
            ScaleCanceling = false;
        }

        private void ExecuteApplyScale(object parameter)
        {
            OldScale = Scale;
            PrintControlView.ScalePreviewPaneVisibility(false);
            ReloadPreview();
        }

        public override void InitializeProperties()
        {
            ResetScale();
            base.InitializeProperties();
        }

        private void ResetScale()
        {
            OldScale = PrintUtility.GetScale();
            Scale = PrintUtility.GetScale();
            PrintControlView.ScalePreviewPaneVisibility(false);
        }

        public override void ReloadPreview()
        {
            if (CurrentPaper != null)
                ReloadPreview(Scale, new Thickness(), PageOrientation, CurrentPaper);
        }

        public void ReloadPreview(double scale, Thickness margin, PageOrientation pageOrientation, PaperSize paperSize)
        {
            try
            {
                //FitToPage_Scale();
                scale = Scale;
                PrintControlView.ScalePreviewPaneVisibility(false);
                ReloadingPreview = true;
                ShowWaitScreen();
                var printSize = GetPrintSize(paperSize, pageOrientation);
                var visual = GetScaledVisual(scale);
                CreatePaginator(visual, printSize, scale);
                var visualPaginator = ((VisualPaginator)Paginator);
                visualPaginator.Initialize(IsMarkPageNumbers);
                PagesAcross = visualPaginator.HorizontalPageCount;

                ApproaxNumberOfPages = MaxCopies = Paginator.PageCount;
                if (Scale == 1)
                    NumberOfPages = ApproaxNumberOfPages;

                DisplayPagePreviewsAll(visualPaginator, scale);
                ReloadingPreview = false;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                WaitScreen.Hide();
            }

        }

        private DrawingVisual GetScaledVisual(double scale)
        {
            if (scale == 1)
                return DrawingVisual;
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.DrawDrawing(DrawingVisual.Drawing);
            }
            return visual;
        }

        private static Size GetPrintSize(PaperSize paperSize, PageOrientation pageOrientation)
        {
            var printSize = new Size(paperSize.Width, paperSize.Height);
            if (pageOrientation == PageOrientation.Landscape)
            {
                printSize = new Size(paperSize.Height, paperSize.Width);
            }
            return printSize;
        }

        private void ShowWaitScreen()
        {
            if (FullScreenPrintWindow != null)
            {
                WaitScreen.Show("������...");
            }
        }

        protected  virtual void CreatePaginator(DrawingVisual visual, Size printSize, double scale)
        {
            Paginator = new VisualPaginator(visual, printSize, new Thickness(), PrintUtility.GetPageMargin(CurrentPrinterName));
        }

        public override void HandlePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (PrintControlViewModel)o;
            switch (e.Property.Name)
            {
                case "Scale":
                    if (presenter.ScaleCanceling)
                        return;
                    ((IPrintControlView)presenter.View).ScalePreviewNode(new ScaleTransform(presenter.Scale, presenter.Scale));
                    presenter.ApproaxNumberOfPages = Convert.ToInt32(Math.Ceiling(presenter.NumberOfPages * presenter.Scale));
                    PrintUtility.SetScale((double)e.NewValue);                    
                    break;

                //case "FitToPage":
                //    FitToPage_Scale();
                //    PrintUtility.SetFitToPage((bool)e.NewValue);
                //    break;
            }
            base.HandlePropertyChanged(o, e);
        }

        public override void ExecutePrint(object parameter)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                printDialog.PrintQueue = CurrentPrinter;
                printDialog.PrintTicket = CurrentPrinter.UserPrintTicket;
                ShowProgressDialog();
                ((VisualPaginator)Paginator).PageCreated += PrintControlPresenterPageCreated;
                string fileName = "���ؼ�¼" + DateTime.Now.ToString("yyyyMMddHHmmss");
				printDialog.PrintDocument(Paginator, fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressDialog.Hide();
            }
        }

        private void PrintControlPresenterPageCreated(object sender, PageEventArgs e)
        {
            ProgressDialog.CurrentProgressValue = e.PageNumber;
            ProgressDialog.Message = GetStatusMessage();
            Application.Current.DoEvents();
        }

        public override void SetProgressDialogCancelButtonVisibility()
        {
            ProgressDialog.CancelButtonVisibility = Visibility.Visible;
        }

        public void ShowPrintPreview()
        {
            if (FullScreenPrintWindow != null)
            {
                FullScreenPrintWindow.Content = null;
            }
            CreatePrintPreviewWindow();
            Loading = true;
            IsSetPrintingOptionsEnabled = false;
            IsCancelPrintingOptionsEnabled = false;
            if (FullScreenPrintWindow != null) FullScreenPrintWindow.ShowDialog();
            ApplicationExtention.MainWindow = null;
        }

        public void ShowPrintPreview(DataTable dataTable)
        {
            DataTableUtil.Validate(dataTable);
        }
        
        public static IPrintControlViewModel Create()
        {
            return new PrintControlViewModel(new PrintControlView());
        }

        public override void FetchSetting()
        {
            base.FetchSetting();

            Scale = PrintUtility.GetScale();
            //FitToPage = PrintUtility.GetFitToPage();
        }
    }
}