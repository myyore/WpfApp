using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp
{
    public static class HighlightBehavior
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(HighlightBehavior),
                new PropertyMetadata(string.Empty, OnTextOrHighlightChanged));

        public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
        public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.RegisterAttached("HighlightText", typeof(string), typeof(HighlightBehavior),
                new PropertyMetadata(string.Empty, OnTextOrHighlightChanged));

        public static string GetHighlightText(DependencyObject obj) => (string)obj.GetValue(HighlightTextProperty);
        public static void SetHighlightText(DependencyObject obj, string value) => obj.SetValue(HighlightTextProperty, value);

        private static void OnTextOrHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                var text = GetText(textBlock);
                var keyword = GetHighlightText(textBlock);

                textBlock.Inlines.Clear();
                if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(text))
                {
                    textBlock.Inlines.Add(new Run(text));
                    return;
                }

                string pattern = Regex.Escape(keyword);
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

                if (matches.Count == 0)
                {
                    textBlock.Inlines.Add(new Run(text));
                    return;
                }

                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        textBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                    }

                    var highlightRun = new Run(match.Value)
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 235, 156)), // 浅黄色高亮
                        Foreground = Brushes.Black,
                        FontWeight = FontWeights.Bold
                    };
                    textBlock.Inlines.Add(highlightRun);
                    lastIndex = match.Index + match.Length;
                }

                if (lastIndex < text.Length)
                {
                    textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
                }
            }
        }
    }
}