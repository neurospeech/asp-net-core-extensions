using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.WKHtmlToXSharp
{

    internal class NFileLock : IDisposable
    {
        readonly FileInfo file;
        readonly FileStream fs;
        private NFileLock(FileInfo file, FileStream fs)
        {
            this.fs = fs;
            this.file = file;
        }

        public static async Task<IDisposable> AcquireAsync(
            string filePath,
            TimeSpan? maxWait = null,
            TimeSpan? poolDelay = null
            )
        {
            FileInfo lockFile = new FileInfo(filePath);

            if (!lockFile.Directory.Exists)
            {
                lockFile.Directory.Create();
            }

            maxWait = maxWait ?? TimeSpan.FromMinutes(15);
            var start = DateTime.UtcNow;

            TimeSpan delay = poolDelay ?? TimeSpan.FromSeconds(5);

            while (true)
            {
                // try to open file...
                try
                {
                    var fs = new FileStream(filePath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.None);
                    fs.Seek(0, SeekOrigin.Begin);
                    await fs.WriteAsync(new byte[] { 1 }, 0, 1);
                    return new NFileLock(lockFile, fs);
                }
                catch
                {

                }

                await Task.Delay(delay);
                var diff = DateTime.UtcNow - start;
                if (diff > maxWait)
                {
                    throw new TimeoutException();
                }
            }
        }

        public void Dispose()
        {
            fs.Dispose();
            // there is a possibility of other process to acquire
            // lock immediately so we will ignore exceptions here
            try
            {
                file.Delete();
            }
            catch { }
        }
    }

    public static class WKHtmlToX
    {

        /// <summary>
        /// Azure compatible temp folder.. setting null will
        /// default it back to Path.GetTempPath()
        /// </summary>
        public static string TempFolder = "d:/temp";

        public static string WKHtmlFolder => $"{TempFolder}/wkhtml";

        public static string GitHubReleaseUrl = "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox-0.12.6-1.mxe-cross-win64.7z";

        private static string ExePath = null;
        private static async Task InstallAsync()
        {
            if (ExePath != null)
                return;

            var exePath = $"{WKHtmlFolder}/bin";

            using (var fileLock = await NFileLock.AcquireAsync(WKHtmlFolder + "/wkhtml.lock")) {

                if (!Directory.Exists(exePath))
                {

                    using (var client = new HttpClient())
                    {
                        var tmpPath = $"{WKHtmlFolder}/tmp.7z";
                        using (var fs = File.OpenWrite(tmpPath))
                        {
                            using (var s = await client.GetStreamAsync(GitHubReleaseUrl))
                            {
                                await s.CopyToAsync(fs);
                            }
                        }

                        using (ArchiveFile archiveFile = new ArchiveFile(tmpPath))
                        {
                            archiveFile.Extract(exePath); // extract all
                        }

                        File.Delete(tmpPath);
                    }
                }
                ExePath = $"{exePath}/wkhtmltox/bin";
            }
        }

        public static async Task<byte[]> HtmlToPdfAsync(string html, ConversionTask options)
        {

            await InstallAsync();

            if (options == null)
            {
                options = new ConversionTask
                {
                    PageSize = Size.A4
                };
            }

            try
            {
                options.CustomSwitches = " --load-error-handling ignore --load-media-error-handling ignore ";
                // return await api.HtmlToPDF(fileName, options.ConversionOptions, System.Text.Encoding.UTF8.GetBytes(html));
                return Convert(ExePath, options.ConversionOptions , html, "wkhtmltopdf.exe");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"options={options.ConversionOptions}\r\nhtml={html}", ex);
            }

        }

        /// <summary>
        /// Converts given URL or HTML string to PDF.
        /// </summary>
        /// <param name="wkhtmlPath">Path to wkthmltopdf\wkthmltoimage.</param>
        /// <param name="switches">Switches that will be passed to wkhtmltopdf binary.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <param name="wkhtmlExe"></param>
        /// <returns>PDF as byte array.</returns>
        static byte[] Convert(string wkhtmlPath, string switches, string html, string wkhtmlExe)
        {
            // switches:
            //     "-q"  - silent output, only errors - no progress messages
            //     " -"  - switch output to stdout
            //     "- -" - switch input to stdin and output to stdout
            switches = "-q " + switches + " -";

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                switches += " -";
                html = SpecialCharsEncode(html);
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(wkhtmlPath, wkhtmlExe),
                    Arguments = switches,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = wkhtmlPath,
                    CreateNoWindow = true
                }
            };
            proc.Start();

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                using (var sIn = proc.StandardInput)
                {
                    sIn.WriteLine(html);
                }
            }

            using (var ms = new MemoryStream())
            {
                using (var sOut = proc.StandardOutput.BaseStream)
                {
                    byte[] buffer = new byte[4096];
                    int read;

                    while ((read = sOut.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                }

                string error = proc.StandardError.ReadToEnd();

                if (ms.Length == 0)
                {
                    throw new Exception(error);
                }

                proc.WaitForExit();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Encode all special chars
        /// </summary>
        /// <param name="text">Html text</param>
        /// <returns>Html with special chars encoded</returns>
        private static string SpecialCharsEncode(string text)
        {
            var chars = text.ToCharArray();
            var result = new StringBuilder(text.Length + (int)(text.Length * 0.1));

            foreach (var c in chars)
            {
                var value = System.Convert.ToInt32(c);
                if (value > 127)
                    result.AppendFormat("&#{0};", value);
                else
                    result.Append(c);
            }

            return result.ToString();
        }
    }

    public class OptionFlag : Attribute
    {
        public string Name { get; private set; }

        public OptionFlag(string name)
        {
            Name = name;
        }
    }


    public class ConversionTask
    {

        public string ConversionOptions
        {
            get
            {
                var result = new StringBuilder();
                if (this.PageMargins != null)
                {
                    result.Append(this.PageMargins.ToString());
                }

                result.Append(" ");

                result.Append(GenerateOptions());

                return result.ToString().Trim();
            }
        }

        private string GenerateOptions()
        {
            var result = new StringBuilder();

            var fields = this.GetType().GetProperties();
            foreach (var fi in fields)
            {
                var of = fi.GetCustomAttributes(typeof(OptionFlag), true).FirstOrDefault() as OptionFlag;
                if (of == null)
                    continue;

                object value = fi.GetValue(this, null);
                if (value == null)
                    continue;

                if (fi.PropertyType == typeof(Dictionary<string, string>))
                {
                    var dictionary = (Dictionary<string, string>)value;
                    foreach (var d in dictionary)
                    {
                        result.AppendFormat(" {0} {1} {2}", of.Name, d.Key, d.Value);
                    }
                }
                else if (fi.PropertyType == typeof(bool))
                {
                    if ((bool)value)
                        result.AppendFormat(CultureInfo.InvariantCulture, " {0}", of.Name);
                }
                else
                {
                    result.AppendFormat(CultureInfo.InvariantCulture, " {0} {1}", of.Name, value);
                }
            }

            return result.ToString().Trim();
        }

        public ConversionTask()
        {
            PageMargins = new Margins();
        }


        /// <summary>
        /// Sets the page margins.
        /// </summary>
        public Margins PageMargins { get; set; }


        /// <summary>
        /// Indicates whether the PDF should be generated in lower quality.
        /// </summary>
        [OptionFlag("-l")]
        public bool IsLowQuality { get; set; }

        /// <summary>
        /// Number of copies to print into the PDF file.
        /// </summary>
        [OptionFlag("--copies")]
        public int? Copies { get; set; }

        /// <summary>
        /// Indicates whether the PDF should be generated in grayscale.
        /// </summary>
        [OptionFlag("-g")]
        public bool IsGrayScale { get; set; }

        /// <summary>
        /// Sets the page size.
        /// </summary>
        [OptionFlag("-s")]
        public Size? PageSize { get; set; }

        /// <summary>
        /// Sets the page width in mm.
        /// </summary>
        /// <remarks>Has priority over <see cref="PageSize"/> but <see cref="PageHeight"/> has to be also specified.</remarks>
        [OptionFlag("--page-width")]
        public double? PageWidth { get; set; }

        /// <summary>
        /// Sets the page height in mm.
        /// </summary>
        /// <remarks>Has priority over <see cref="PageSize"/> but <see cref="PageWidth"/> has to be also specified.</remarks>
        [OptionFlag("--page-height")]
        public double? PageHeight { get; set; }

        /// <summary>
        /// Sets the page orientation.
        /// </summary>
        [OptionFlag("-O")]
        public Orientation? PageOrientation { get; set; }


        /// <summary>
        /// Sets custom headers.
        /// </summary>
        [OptionFlag("--custom-header")]
        public Dictionary<string, string> CustomHeaders { get; set; }

        /// <summary>
        /// Sets cookies.
        /// </summary>
        [OptionFlag("--cookie")]
        public Dictionary<string, string> Cookies { get; set; }

        /// <summary>
        /// Sets post values.
        /// </summary>
        [OptionFlag("--post")]
        public Dictionary<string, string> Post { get; set; }

        /// <summary>
        /// Indicates whether the page can run JavaScript.
        /// </summary>
        [OptionFlag("-n")]
        public bool IsJavaScriptDisabled { get; set; }

        /// <summary>
        /// Minimum font size.
        /// </summary>
        [OptionFlag("--minimum-font-size")]
        public int? MinimumFontSize { get; set; }

        /// <summary>
        /// Sets proxy server.
        /// </summary>
        [OptionFlag("-p")]
        public string Proxy { get; set; }

        /// <summary>
        /// HTTP Authentication username.
        /// </summary>
        [OptionFlag("--username")]
        public string UserName { get; set; }

        /// <summary>
        /// HTTP Authentication password.
        /// </summary>
        [OptionFlag("--password")]
        public string Password { get; set; }

        /// <summary>
        /// Use this if you need another switches that are not currently supported by Rotativa.
        /// </summary>
        [OptionFlag("")]
        public string CustomSwitches { get; set; }

    }

    public class Margins
    {
        /// <summary>
        /// Page bottom margin in mm.
        /// </summary>
        [OptionFlag("-B")] public int? Bottom;

        /// <summary>
        /// Page left margin in mm.
        /// </summary>
        [OptionFlag("-L")] public int? Left;

        /// <summary>
        /// Page right margin in mm.
        /// </summary>
        [OptionFlag("-R")] public int? Right;

        /// <summary>
        /// Page top margin in mm.
        /// </summary>
        [OptionFlag("-T")] public int? Top;

        public Margins()
        {
        }

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="top">Page top margin in mm.</param>
        /// <param name="right">Page right margin in mm.</param>
        /// <param name="bottom">Page bottom margin in mm.</param>
        /// <param name="left">Page left margin in mm.</param>
        public Margins(int top, int right, int bottom, int left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            FieldInfo[] fields = GetType().GetFields();
            foreach (FieldInfo fi in fields)
            {
                var of = fi.GetCustomAttributes(typeof(OptionFlag), true).FirstOrDefault() as OptionFlag;
                if (of == null)
                    continue;

                object value = fi.GetValue(this);
                if (value != null)
                    result.AppendFormat(CultureInfo.InvariantCulture, " {0} {1}", of.Name, value);
            }

            return result.ToString().Trim();
        }
    }

    /// <summary>
    /// Page size.
    /// </summary>
    public enum Size
    {
        /// <summary>
        /// 841 x 1189 mm
        /// </summary>
        A0,

        /// <summary>
        /// 594 x 841 mm
        /// </summary>
        A1,

        /// <summary>
        /// 420 x 594 mm
        /// </summary>
        A2,

        /// <summary>
        /// 297 x 420 mm
        /// </summary>
        A3,

        /// <summary>
        /// 210 x 297 mm
        /// </summary>
        A4,

        /// <summary>
        /// 148 x 210 mm
        /// </summary>
        A5,

        /// <summary>
        /// 105 x 148 mm
        /// </summary>
        A6,

        /// <summary>
        /// 74 x 105 mm
        /// </summary>
        A7,

        /// <summary>
        /// 52 x 74 mm
        /// </summary>
        A8,

        /// <summary>
        /// 37 x 52 mm
        /// </summary>
        A9,

        /// <summary>
        /// 1000 x 1414 mm
        /// </summary>
        B0,

        /// <summary>
        /// 707 x 1000 mm
        /// </summary>
        B1,

        /// <summary>
        /// 500 x 707 mm
        /// </summary>
        B2,

        /// <summary>
        /// 353 x 500 mm
        /// </summary>
        B3,

        /// <summary>
        /// 250 x 353 mm
        /// </summary>
        B4,

        /// <summary>
        /// 176 x 250 mm
        /// </summary>
        B5,

        /// <summary>
        /// 125 x 176 mm
        /// </summary>
        B6,

        /// <summary>
        /// 88 x 125 mm
        /// </summary>
        B7,

        /// <summary>
        /// 62 x 88 mm
        /// </summary>
        B8,

        /// <summary>
        /// 33 x 62 mm
        /// </summary>
        B9,

        /// <summary>
        /// 31 x 44 mm
        /// </summary>
        B10,

        /// <summary>
        /// 163 x 229 mm
        /// </summary>
        C5E,

        /// <summary>
        /// 105 x 241 mm - U.S. Common 10 Envelope
        /// </summary>
        Comm10E,

        /// <summary>
        /// 110 x 220 mm
        /// </summary>
        Dle,

        /// <summary>
        /// 190.5 x 254 mm
        /// </summary>
        Executive,

        /// <summary>
        /// 210 x 330 mm
        /// </summary>
        Folio,

        /// <summary>
        /// 431.8 x 279.4 mm
        /// </summary>
        Ledger,

        /// <summary>
        /// 215.9 x 355.6 mm
        /// </summary>
        Legal,

        /// <summary>
        /// 215.9 x 279.4 mm
        /// </summary>
        Letter,

        /// <summary>
        /// 279.4 x 431.8 mm
        /// </summary>
        Tabloid
    }

    /// <summary>
    /// Page orientation.
    /// </summary>
    public enum Orientation
    {
        Landscape,
        Portrait
    }

    /// <summary>
    /// Image output format
    /// </summary>
    public enum ImageFormat
    {
        jpeg,
        png
    }

}
