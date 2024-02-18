using LiZhenMySQL;
using System.Collections.ObjectModel;

namespace ICE_Model
{
    /// <summary>
    /// 评论抽象基类
    /// </summary>
    public abstract class Opinion:Tree<Opinion>,IDbObject
    {
        public Person Publisher { get; set; }
        public ObservableCollection<Person> Ats { get; } = new ObservableCollection<Person>();
        public string Content { get; set; }
    }

    public class Opinion_Project : Opinion
    {
        public int ID_Project { get; set; }
    }
    public class Opinion_Task : Opinion
    {
        public int ID_Task { get; set; }
    }

}
