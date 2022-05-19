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
        private static readonly TextChangedEventHandler onTextChanged = new(OnTextChanged);
        private static readonly KeyboardFocusChangedEventHandler onLostKeyboardFocus = new(OnLostKeyboardFocus);
        private static readonly KeyEventHandler onKeyDown = new(OnPreviewKeyDown);
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
        public static IEnumerable<string>? GetItemsSource(DependencyObject obj)
        {
            object objRtn = obj.GetValue(ItemsSource);
            if (objRtn is IEnumerable<string>)
                return (objRtn as IEnumerable<string>);

            return null;
        }
        public static void SetItemsSource(DependencyObject obj, IEnumerable<string> value) => obj.SetValue(ItemsSource, value);
        private static void OnItemsSource(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == null || sender is not TextBox tb)
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
                typeof(char?),
                typeof(AutoComplete),
                new UIPropertyMetadata(null)
            );
        public static char? GetPrefix(DependencyObject obj) => (char?)obj.GetValue(Prefix);
        public static void SetPrefix(DependencyObject obj, char? value) => obj.SetValue(Prefix, value);
        #endregion Prefix

        #region CommitKey
        /// <summary>
        /// Determines which key to use as the commit key.
        /// </summary>
        /// <remarks>Defaults to <see cref="Key.Enter"/>.</remarks>
        public static readonly DependencyProperty CommitKey =
            DependencyProperty.RegisterAttached
            (
                "CommitKey",
                typeof(Key),
                typeof(AutoComplete),
                new UIPropertyMetadata(Key.Enter)
            );
        public static Key GetCommitKey(DependencyObject obj) => (Key)obj.GetValue(CommitKey);
        public static void SetCommitKey(DependencyObject obj, Key key) => obj.SetValue(CommitKey, key);
        #endregion CommitKey
        #endregion DependencyProperties

        #region EventHandlers
        /// <summary>
        /// Used for moving the caret to the end of the suggested auto-completion text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is not TextBox tb)
                return;

            Key commitKey = GetCommitKey(tb);

            if (!e.Key.Equals(commitKey))
                return;

            //If we pressed enter and if the selected text goes all the way to the end, move our caret position to the end
            if (tb.SelectionLength > 0 && (tb.SelectionStart + tb.SelectionLength == tb.Text.Length))
            {
                tb.SelectionStart = tb.CaretIndex = tb.Text.Length;
                tb.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Used for the same effect as <see cref="OnPreviewKeyDown(object, KeyEventArgs)"/>, but for when we lose keyboard focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OriginalSource is not TextBox tb)
                return;

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
            if (sender == null || sender is not TextBox tb || ((from change in e.Changes where change.RemovedLength > 0 select change).Any() && (from change in e.Changes where change.AddedLength > 0 select change).Any() == false))
                return;

            var values = GetItemsSource(tb);

            //No reason to search if we don't have any values.
            if (values == null)
                return;

            //No reason to search if there's nothing there.
            if (string.IsNullOrEmpty(tb.Text))
                return;

            char? prefix = GetPrefix(tb);
            int prefixStartIndex = 0, matchStartIndex = 0;
            string matchingString = tb.Text;
            //If we have a trigger string, make sure that it has been typed before
            //giving auto-completion suggestions.
            if (prefix != null)
            {
                prefixStartIndex = tb.Text.LastIndexOf(prefix.Value);
                //If we haven't typed the trigger string, then don't do anything.
                if (prefixStartIndex == -1)
                    return;

                matchStartIndex = prefixStartIndex + 1;
                matchingString = tb.Text[matchStartIndex..];
            }

            //If we don't have anything after the trigger string, return.
            if (string.IsNullOrEmpty(matchingString))
                return;

            int textLength = matchingString.Length;

            StringComparison comparer = GetStringComparisonMode(tb);
            //Do search and changes here.
            string? match =
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
                where value[..textLength].Equals(matchingString, comparer)
                select value[textLength..]/*Only select the last part of the suggestion*/
            ).FirstOrDefault();

            //Nothing.  Leave 'em alone
            if (string.IsNullOrEmpty(match))
                return;

            tb.TextChanged -= onTextChanged; //< disable text changed event trigger

            int matchStart = (matchStartIndex + matchingString.Length);
            tb.Text += match;
            tb.CaretIndex = matchStart;
            tb.SelectionStart = matchStart;
            tb.SelectionLength = (tb.Text.Length - matchStartIndex);

            tb.TextChanged += onTextChanged; //< enable text changed event trigger
        }
        #endregion EventHandlers
    }
}
