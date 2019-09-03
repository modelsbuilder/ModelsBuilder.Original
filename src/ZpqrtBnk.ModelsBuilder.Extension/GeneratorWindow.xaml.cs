using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using ZpqrtBnk.ModelsBuilder.Api;

namespace ZpqrtBnk.ModelsBuilder.Extension
{
    /// <summary>
    /// Interaction logic for GeneratorWindow.xaml
    /// </summary>
    public partial class GeneratorWindow : DialogWindow
    {
        private readonly AsyncPackage _package;
        private readonly ProjectItem _sourceItem;

        private readonly StringBuilder _text = new StringBuilder();
        private int _progress;

        public GeneratorWindow(AsyncPackage package, ProjectItem sourceItem)
        {
            _package = package;
            _sourceItem = sourceItem;

            InitializeComponent();

            ButtonClose.IsEnabled = false;

            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = 100;

            Text.IsReadOnly = true;
            Text.TextWrapping = TextWrapping.Wrap;
            Text.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Text.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            WriteLine("Models Builder " + ApiVersion.Current.Version);
            WriteLine();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_progress < 100)
                e.Cancel = true;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            //var time = Stopwatch.StartNew();
            var generator = new Generator(_package, _sourceItem);
            generator.Progressed += (message, percent) => // invoked on main thread
            {
                //Write($"[{time.ElapsedMilliseconds:000000}] ");
                WriteLine(message);
                Progress(percent);
            };

            // not too proud of that async hackish code
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await generator.GenerateAsync();
            });
        }

        public void Write(string text)
        {
            _text.Append(text);
            Text.Text = _text.ToString();
            Render(Text);
        }

        public void WriteLine(string text = null)
        {
            if (!string.IsNullOrWhiteSpace(text))
                _text.Append(text);
            _text.AppendLine();
            Text.Text = _text.ToString();
            Text.ScrollToEnd();
            Render(Text);
        }

        public void Progress(int progress)
        {
            if (progress <= _progress)
                return;

            ProgressBar.Value = progress;
            Render(ProgressBar);
            _progress = progress;

            if (progress == 100)
                ButtonClose.IsEnabled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // not too proud of that one, but how do you refresh the UI?
        // https://www.meziantou.net/refresh-a-wpf-control.htm
        // https://www.c-sharpcorner.com/article/update-ui-with-wpf-dispatcher-and-tpl/
        private static void Render(DispatcherObject dispatcherObject)
        {
            #pragma warning disable VSTHRD001 // bah
            dispatcherObject.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
        }
    }
}
