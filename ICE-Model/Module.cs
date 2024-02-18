using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using LiZhenMySQL;

namespace ICE_Model
{
    /// <summary>
    /// 模块
    /// </summary>
    [Serializable]
    public class Module : IDbObject, INamedObject
    {
        public object ID { get; set; }
        public string Name { get; set; }
        public string Illustration { get; set; }
    }

    /// <summary>
    /// 模块关联
    /// </summary>
    public class ModuleChain:IDbObject
    {
        public object ID { get; set; }

        public Model Model { get; set; }
        public Module SourceModule { get; set; }
        public Module TargetModule { get; set; }
        public ModuleChainType ModuleChainType { get; set; }
    }

    /// <summary>
    /// 模块关联模式：映射（一对一），父子（一对多），引用（多对多）
    /// </summary>
    public enum ModuleChainType { Parent, Quote, Mapping }

    public class Model:IDbObject,INamedObject
    {
        public object ID { get; set; }
        public string Name { get; set; }
        public string Illustration { get; set; }

    }
}

