using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace ProjectApp.Utils
{
    public static class CollectionViewHelper
    {
        public static ICollectionView CreateFilteredView<T>(
            ObservableCollection<T> source,
            Func<T, string, bool> matchFunc,
            Func<string> getSearchText)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (matchFunc == null) throw new ArgumentNullException(nameof(matchFunc));
            if (getSearchText == null) throw new ArgumentNullException(nameof(getSearchText));

            var view = CollectionViewSource.GetDefaultView(source);
            view.Filter = item => matchFunc((T)item, getSearchText());
            return view;
        }
    }
}