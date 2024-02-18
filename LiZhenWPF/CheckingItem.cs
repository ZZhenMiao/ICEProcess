using LiZhenMySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiZhenStandard.Extensions;
using LiZhenStandard;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Specialized;

namespace LiZhenWPF
{
    public interface ICheckingItem: INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }
        public virtual string Name { get => (string)Content.GetPropertyValue("Name"); set => Content.SetPropertyValue("Name", value); }
        public Type ContentType { get; set; }
        public IDbObject Content { get; set; }
        public bool IsEnabled { get; set; }

        public virtual string ToString() { return Name; }
    }

    public class CheckingItem : ICheckingItem
    {
        private bool isSelected;
        private bool isEnabled = true;

        public bool IsSelected { get => isSelected; set 
            { 
                isSelected = value; 
                if(PropertyChanged is not null)
                PropertyChanged(this, new PropertyChangedEventArgs("IsSelected"));
            } }
        public string Name { get => (string)Content.GetPropertyValue("Name"); set => Content.SetPropertyValue("Name", value); }
        public Type ContentType { get; set; }
        public IDbObject Content { get; set; }
        public bool IsEnabled
        {
            get => isEnabled; set
            {
                isEnabled = value;
                if (PropertyChanged is not null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsEnabled"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }
    }

    public class CheckingTreeItem : Tree<CheckingTreeItem>, ICheckingItem
    {
        public bool isSelected;
        private bool isEnabled = true;

        public bool IsSelected { get => isSelected; set { isSelected = value;
                if (PropertyChanged is not null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsSelected")); } }
        public string Name { get => (string)Content.GetPropertyValue("Name"); set => Content.SetPropertyValue("Name", value); }
        public Type ContentType { get; set; }
        public IDbObject Content { get; set; }
        public bool IsEnabled
        {
            get => isEnabled; set
            {
                isEnabled = value;
                if (PropertyChanged is not null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsEnabled"));
            }
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }

        //public CheckingTreeItem Parent { get; set; }
        //public ObservableCollection<CheckingTreeItem> Children { get; } = new ObservableCollection<CheckingTreeItem>();

        public static CheckingTreeItem[] MakeTreeItem<T>(IEnumerable<T> trees, CheckingTreeItem parent = null) where T : DBTree<T>
        {
            if (trees == null)
                return new CheckingTreeItem[0];
            List<CheckingTreeItem> re = new List<CheckingTreeItem>();
            foreach (T item in trees)
            {
                CheckingTreeItem treeItem = new CheckingTreeItem() { Parent = parent, Content = item };
                re.Add(treeItem);
                if (item.Children.Any())
                    treeItem.Children.AddRange(MakeTreeItem(item.Children, treeItem));
            }
            return re.ToArray();
        }
        public static CheckingTreeItem[] GetAllNodes<T>(T tree) where T : CheckingTreeItem
        {
            List<CheckingTreeItem> re = new List<CheckingTreeItem>();
            re.Add(tree);
            foreach (T item in tree.Children)
            {
                re.Add(item);
                if (item.Children.Any())
                    re.AddRange(GetAllNodes(item));
            }
            return re.ToArray();
        }
    }
}
