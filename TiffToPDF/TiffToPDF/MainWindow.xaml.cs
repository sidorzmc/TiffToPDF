using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace TiffToPDF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/TiffToPDF;component/loading.gif");
            image.EndInit();
            ImageBehavior.SetAnimatedSource(image1, image);
            image1.Visibility = Visibility.Collapsed; 
        }

        private string folderePath = "";
        private int counter_total;
        private int counter = 1;
        string log_path;


        private void btn_browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                string[] files = Directory.GetFiles(fbd.SelectedPath, @"*.tif*");
                tb_browse.Text = folderePath = fbd.SelectedPath;
                lbl.Content = "Tiff files found: " + files.Length;
            }
            
        }

        private void frame_Navigated(object sender, NavigationEventArgs e)
        {

        }

       

        private void btn_convert_Click(object sender, RoutedEventArgs e)
        {
            
            if (folderePath != "")
            {
                image1.Visibility = Visibility.Visible;
                DirectoryInfo tiffDir = new DirectoryInfo(folderePath);
                FileInfo[] tiffFiles = tiffDir.GetFiles(@"*.tif*");

                //Parallel.ForEach(tiffFiles, tiffFile =>
                //{
                //    string pdfFile = System.IO.Path.ChangeExtension(tiffFile.FullName, ".pdf");
                //    Convert(tiffFile.FullName, System.Convert.ToInt64(textBox.Text));

                //    List<object> arguments = new List<object>(); //adds objects to the list, to pass into the background worker
                //    arguments.Add(tiffFile.FullName);
                //    if (backgroundWorker1.IsBusy != true) //if the backgroundWorker isn't running
                //    {
                //        backgroundWorker1.RunWorkerAsync(arguments); //Start the asynchronous operation.
                //    }
                //});

                counter_total = tiffFiles.Count();                
                log_path = tb_browse.Text + "//log.txt";
                long ie =  System.Convert.ToInt64(textBox.Text);   

                foreach (FileInfo tiffFile in tiffFiles)
                {
                    string pdfFile = System.IO.Path.ChangeExtension(tiffFile.FullName, ".pdf");
                    Convert(tiffFile.FullName, System.Convert.ToInt64(textBox.Text));
                }
                image1.Visibility = Visibility.Collapsed;
            }
            else { lbl.Content = "Chose Directory"; }
        }

       

        private void Convert (string filename, long compression_level)
        {            
            try
            {

                Dispatcher.Invoke(new Action(() =>
                {
                    
                    if (!File.Exists(log_path))
                    {
                        using (File.Create(log_path)) ;
                    }
                }), DispatcherPriority.ContextIdle);

                //string log_path = tb_browse.Text + "//log.txt";
                //if (!File.Exists(log_path)) {
                //    using (File.Create(log_path)) ;
                //}
                string destinaton ="";
                string strTemp = System.IO.Path.GetExtension(filename).ToLower();

                if (strTemp == ".tif")
                {
                    destinaton = System.IO.Path.ChangeExtension(filename, "pdf");
                }
                if (strTemp == ".tiff")
                {
                    destinaton = System.IO.Path.ChangeExtension(filename, "pdf");
                }              
                
                System.Drawing.Image image = System.Drawing.Image.FromFile(filename);
                PdfDocument doc = new PdfDocument();
                XGraphics xgr;
                int count = image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Page);
                for (int pageNum = 0; pageNum < count; pageNum++)
                {                    
                    image.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, pageNum);
                    System.Drawing.Image image2;
                    var ms = new MemoryStream();
                    var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                    var encParams = new EncoderParameters() { Param = new[] { new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, compression_level) } };
                    image.Save(ms, encoder, encParams);
                    image2 = System.Drawing.Image.FromStream(ms);
                    PdfPage page = new PdfPage();
                    doc.Pages.Add(page);
                    xgr = XGraphics.FromPdfPage(page);

                    // if (image2.Height == image2.Width)
                    //return MyAspectEnum.Square;
                    if (image2.Height > image2.Width)
                        page.Orientation = PdfSharp.PageOrientation.Portrait;
                    else page.Orientation = PdfSharp.PageOrientation.Landscape;

                    XImage ximg = XImage.FromGdiPlusImage(image2);
                    xgr.DrawImage(ximg, 0, 0);                    
                }                
                doc.Save(destinaton);
                doc.Close();
                image.Dispose();


                // Dispatcher.Invoke(new Action(() => { rtb_log.AppendText("\nFile " + filename + " converted successfully!"); }), DispatcherPriority.ContextIdle);

                Dispatcher.Invoke(new Action(() =>
                {
                    rtb_log.AppendText("\nFile " + filename + " converted successfully!");
                   
                }), DispatcherPriority.ContextIdle);



                File.AppendAllText(  log_path, Environment.NewLine + DateTime.Now.ToString() + " File " + filename + " converted successfully!");

                label3.Content = "Files converted: "+ counter+"/"+counter_total;
                counter++;
                
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    rtb_log.AppendText("Error " + ex.Message);
                }), DispatcherPriority.ContextIdle);
            }
            
        }

        
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string previousInput = "100";
            Regex r = new Regex(@"^-{0,1}\d+\.{0,1}\d*$"); 
            Match m = r.Match(textBox.Text);
            if (m.Success)
            {
                long i = System.Convert.ToInt64(textBox.Text);
                if (i > 100 ) { textBox.Text = previousInput; }               
            }
            else
            {
                textBox.Text = previousInput;
            }
        }
    }
}
