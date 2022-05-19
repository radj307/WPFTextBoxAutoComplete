using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPFTextBoxAutoComplete
{
    public static class AutoComplete
    {
        #region Fields
        private static readonly TextChangedEventHandler onTextChanged = new TextChangedEventHandler(OnTextChanged);
        private static readonly KeyEventHandler onKeyDown = new KeyEventHandler(OnPreviewKeyDown);
        #endregion Fields

        #region DependencyProperties
        #region ItemsSource
        /// <summary>
        /// The collection to search for matches from.
        /// </summary>
        public static readonly DependencyProperty ItemsSource =
            DependencyProperty.RegisterAttached
            (
                "ItemsSource",
                typeof(IEnumerable<string>),
                typeof(AutoComplete),
                new UIPropertyMetadata(null, OnItemsSource)
            );
        public static IEnumerable<string> GetItemsSource(DependencyObject obj)
        {
            object objRtn = obj.GetValue(ItemsSource);
            if (objRtn is IEnumerable<string>)
                return (objRtn as IEnumerable<string>);

            return null;
        }
        public static void SetItemsSource(DependencyObject obj, IEnumerable<string> value) => obj.SetValue(ItemsSource, value);
        private static void OnItemsSource(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (sender == null)
                return;

            //If we're being removed, remove the callbacks
            //Remove our old handler, regardless of if we have a new one.
            tb.TextChanged -= onTextChanged;
            tb.PreviewKeyDown -= onKeyDown;
            if (e.NewValue != null)
            {
                //New source.  Add the callbacks
                tb.TextChanged += onTextChanged;
                tb.PreviewKeyDown += onKeyDown;
            }
        }
        #endregion ItemsSource

        #region StringComparisonMode
        /// <summary>
        /// Whether or not to ignore case when searching for matches.
        /// </summary>
        public static readonly DependencyProperty StringComparisonMode =
            DependencyProperty.RegisterAttached
            (
                "StringComparisonMode",
                typeof(StringComparison),
                typeof(AutoComplete),
                new UIPropertyMetadata(StringComparison.Ordinal)
            );
        public static StringComparison GetStringComparisonMode(DependencyObject obj) => (StringComparison)obj.GetValue(StringComparisonMode);
        public static void SetStringComparisonMode(DependencyObject obj, StringComparison value) => obj.SetValue(StringComparisonMode, value);
        #endregion StringComparisonMode

        #region Prefix
        /// <summary>
        /// Setting this to any non-empty string will require the user to type it before auto-complete will show suggestions.
        /// </summary>
        /// <remarks>You can use the <see cref="RemovePrefixOnSelect"/> property to specify whether or not the prefix string is retained when the user selects a suggestion.</remarks>
        public static readonly DependencyProperty Prefix =
            DependencyProperty.RegisterAttached
            (
                "Prefix",
                typeof(string),
                typeof(AutoComplete),
                new UIPropertyMetadata(string.Empty)
            );
        public static string GetPrefix(DependencyObject obj) => (string)obj.GetValue(Prefix);
        public static void SetPrefix(DependencyObject obj, string value) => obj.SetValue(Prefix, value);
        #endregion Prefix

        #region RemovePrefixOnSelect
        /// <summary>
        /// Whether or not to remove the <see cref="Prefix"/> string when the user selects an auto-complete suggestion.
        /// </summary>
        public static readonly DependencyProperty RemovePrefixOnSelect =
            DependencyProperty.RegisterAttached
            (
                "RemovePrefixOnSelect",
                typeof(bool),
                typeof(AutoComplete),
                new UIPropertyMetadata(false)
            );
        public static bool GetRemovePrefixOnSelect(DependencyObject obj) => Convert.ToBoolean(obj.GetValue(RemovePrefixOnSelect));
        public static void SetRemovePrefixOnSelect(DependencyObject obj, bool value) => obj.SetValue(RemovePrefixOnSelect, value);
        #endregion RemovePrefixOnSelect
        #endregion DependencyProperties

        #region EventHandlers
        /// <summary>
        /// Used for moving the caret to the end of the suggested auto-completion text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (!(e.OriginalSource is TextBox tb))
                return;

            //If we pressed enter and if the selected text goes all the way to the end, move our caret position to the end
            if (tb.SelectionLength > 0 && (tb.SelectionStart + tb.SelectionLength == tb.Text.Length))
            {
                tb.SelectionStart = tb.CaretIndex = tb.Text.Length;
                tb.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Search for auto-completion suggestions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if
            (
                (from change in e.Changes where change.RemovedLength > 0 select change).Any() &&
                (from change in e.Changes where change.AddedLength > 0 select change).Any() == false
            )
                return;

            TextBox tb = e.OriginalSource as TextBox;
            if (sender == null)
                return;

            IEnumerable<string> values = GetItemsSource(tb);

            //No reason to search if we don't have any values.
            if (values == null)
                return;

            //No reason to search if there's nothing there.
            if (string.IsNullOrEmpty(tb.Text))
                return;

            string indicator = GetPrefix(tb);
            int startIndex = 0; //Start from the beginning of the line.
            string matchingString = tb.Text;
            //If we have a trigger string, make sure that it has been typed before
            //giving auto-completion suggestions.
            if (!string.IsNullOrEmpty(indicator))
            {
                startIndex = tb.Text.LastIndexOf(indicator);
                //If we haven't typed the trigger string, then don't do anything.
                if (startIndex == -1)
                    return;

                startIndex += indicator.Length;
                matchingString = tb.Text.Substring(startIndex, (tb.Text.Length - startIndex));
            }

            //If we don't have anything after the trigger string, return.
            if (string.IsNullOrEmpty(matchingString))
                return;

            int textLength = matchingString.Length;

            StringComparison comparer = GetStringComparisonMode(tb);
            //Do search and changes here.
            string match =
            (
                from
                    value
                in
                (
                    from subvalue
                    in values
                    where subvalue != null && subvalue.Length >= textLength
                    select subvalue
                )
                where value.Substring(0, textLength).Equals(matchingString, comparer)
                select value.Substring(textLength, value.Length - textLength)/*Only select the last part of the suggestion*/
            ).FirstOrDefault();

            //Nothing.  Leave 'em alone
            if (string.IsNullOrEmpty(match))
                return;

            int matchStart = (startIndex + matchingString.Length);
            tb.TextChanged -= onTextChanged;
            tb.Text += match;
            tb.CaretIndex = matchStart;
            tb.SelectionStart = matchStart;
            tb.SelectionLength = (tb.Text.Length - startIndex);
            tb.TextChanged += onTextChanged;
        }
        #endregion EventHandlers
    }
}
