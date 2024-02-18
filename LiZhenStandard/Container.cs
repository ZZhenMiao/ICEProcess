using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LiZhen.Extensions;

namespace LiZhenStandard
{
    public class ContainerAddedEventArgs : EventArgs
    {
        public object[] NewChilds { get; set; }
    }
    public delegate void ContainerAddedEventHandler(object sender, ContainerAddedEventArgs e);

    public class ContainerRemovedEventArgs : EventArgs
    {
        public object[] OldChilds { get; set; }
    }
    public delegate void ContainerRemovedEventHandler(object sender, ContainerRemovedEventArgs e);

    public class ContainerSelectChangedEventArgs : EventArgs
    {
        public object[] SelectItems { get; set; }
    }
    public delegate void ContainerSelectChangedEventHandler(object sender, ContainerSelectChangedEventArgs e);

    public interface IContainer<T>:IEnumerable<T>
    {
        event ContainerAddedEventHandler ChildAdd;
        event ContainerRemovedEventHandler ChildRemoved;

        void OnSelectChanged(object sender, ContainerSelectChangedEventArgs e);
        void OnAdded(object sender, ContainerAddedEventArgs e);
        void OnRemoved(object sender, ContainerRemovedEventArgs e);
    }

    public class Container<T> : List<T>, IContainer<T>
    {
        //protected readonly List<T> _list = new List<T>();

        public string ElementNamePropertyName { get; set; }
        public string ElementIDPropertyName { get; set; }
        public virtual T this[string name] => Find(a => a.GetPropertyValue(string.IsNullOrWhiteSpace(ElementNamePropertyName) ? "Name" : ElementNamePropertyName).ToString() == name);
        public virtual T this[object ID] => Find(a => a.GetPropertyValue(string.IsNullOrWhiteSpace(ElementIDPropertyName) ? "ID" : ElementIDPropertyName) == ID);

        public event ContainerAddedEventHandler ChildAdd;
        public event ContainerRemovedEventHandler ChildRemoved;
        public event ContainerSelectChangedEventHandler SelectChanged;

        public virtual void OnSelectChanged(object sender, ContainerSelectChangedEventArgs e) => SelectChanged?.Invoke(sender, e);
        public virtual void OnAdded(object sender, ContainerAddedEventArgs e) => ChildAdd?.Invoke(sender, e);
        public virtual void OnRemoved(object sender, ContainerRemovedEventArgs e) => ChildRemoved?.Invoke(sender, e);

        protected virtual void Push(T item)
        {
            base.Add(item);
        }
        protected virtual void PushRange(IEnumerable<T> items)
        {
            base.AddRange(items);
        }
        protected virtual void BaseClear()
        {
            base.Clear();
        }
        public new virtual void AddRange(IEnumerable<T> items)
        {
            base.AddRange(items);
            OnAdded(this, new ContainerAddedEventArgs() { NewChilds = items.ConvertAll(new Func<T, object>((a) => { return a; })) });
        }
        public new virtual void Add(T item)
        {
            base.Add(item);
            OnAdded(this, new ContainerAddedEventArgs() { NewChilds = new object[1] { item } });
        }
        public new virtual void Clear()
        {
            OnRemoved(this, new ContainerRemovedEventArgs() { OldChilds = this.ConvertAll(new Func<T, object>((a) => { return a; })) });
            base.Clear();
        }
        public new virtual void Insert(int index, T item)
        {
            OnAdded(this, new ContainerAddedEventArgs() { NewChilds = new object[1] { item } });
            base.Insert(index, item);
        }
        public new virtual bool Remove(T item)
        {
            OnRemoved(this, new ContainerRemovedEventArgs() { OldChilds = new object[1] { item } });
            return base.Remove(item);
        }
        public new virtual void RemoveAt(int index)
        {
            OnRemoved(this, new ContainerRemovedEventArgs() { OldChilds = new object[1] { this[index] } });
            base.RemoveAt(index);
        }

        public Container() { }
        public Container(List<T> list) { AddRange(list); }
    }

    public static Container<T> ToContainer<T>(this List<T> list) => new Container<T>(list);

}
