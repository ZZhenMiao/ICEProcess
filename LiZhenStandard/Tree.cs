using LiZhenStandard.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LiZhenStandard
{
    public class TreeParentChangedEventArgs : EventArgs
    {
        public object OldParent { get; set; }
        public object NewParent { get; set; }
    }
    public delegate void TreeParentChangedEventHandler(object sender, TreeParentChangedEventArgs e);

    public interface ITree<T> : IEnumerable<T>,INotifyPropertyChanged
    {
        T Parent { get; set; }
        T this[int i] { get; }

        T GetRoot();
        void Add(T ts);
        IEnumerable<T> GetParentNodes();
        IEnumerable<T> GetAllChildren();
        void OnTreeParentChanged(object sender, TreeParentChangedEventArgs e);

        event TreeParentChangedEventHandler TreeParentChanged;
    }
    public abstract class Tree<T> :ObservableCollection<T>, ITree<T> where T : Tree<T>
    {
        protected T parent;
        public event TreeParentChangedEventHandler TreeParentChanged;

        public virtual T Parent { get => parent; set 
            {
                T old = parent;
                parent = value;
                OnTreeParentChanged(this, new TreeParentChangedEventArgs { OldParent = old, NewParent = value });
                OnPropertyChanged(new PropertyChangedEventArgs("Parent"));
            } }
        public virtual void OnTreeParentChanged(object sender, TreeParentChangedEventArgs e)
        {
            TreeParentChanged?.Invoke(sender, e);
        }
        public ObservableCollection<T> Children { get => this; }

        public virtual T GetRoot()
        {
            if (parent is null)
                return (T)this;
            else
                return parent.GetRoot();
        }
        public IEnumerable<T> GetParentNodes()
        {
            List<T> re = new List<T>();
            re.AddRange(GetParentNodesWithSelf());
            re.Remove((T)this);
            return re;
        }
        public IEnumerable<T> GetParentNodesWithSelf()
        {
            List<T> re = new List<T>() { (T)this };
            if (!(Parent is null))
                re.AddRange(Parent.GetParentNodesWithSelf());
            return re;
        }
        public IEnumerable<T> GetAllChildren()
        {
            List<T> re = new List<T>();
            foreach (T child in Children)
            {
                re.Add(child);
                if (child.Children.Any())
                    re.AddRange(child.GetAllChildren());
            }
            return re;
        }
        public IEnumerable<T> GetAllChildrenWithSelf()
        {
            List<T> re = new List<T>();
            re.Add((T)this);
            re.AddRange(GetAllChildren());
            return re;
        }
    }
}