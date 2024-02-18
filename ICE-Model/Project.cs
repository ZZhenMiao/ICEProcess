using LiZhenMySQL;
using System;
using System.Collections.ObjectModel;

namespace ICE_Model
{
    /// <summary>
    /// 项目
    /// </summary>
    [SqlObject]
    [Serializable]
    public class Project: Tree<Project>, IDbObject, INamedObject
    {
        [SqlProperty]
        public int? ID_Module { get; set; }
        [SqlProperty]
        public int? ID_Process { get; set; }
        public Process Process { get; set; }
        public Module Module { get; set; }

        private string name;
        [SqlProperty]
        public override string Name
        {
            get
            {
                return string.IsNullOrWhiteSpace(name) ? Process.NameExpression.Analesize(this) : name;
            }
            set
            {
                name = value;
            }
        }

    }


}
