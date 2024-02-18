using LiZhenMySQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE_Model
{
    [SqlObject]
    [Serializable]
    public class ProjectDirectory:AssetDirectory
    {

    }

    [SqlObject]
    [Serializable]
    public class ProjectFile : DbNamedObject
    {

    }

    [SqlObject]
    [Serializable]
    public class ProjectTree : ProjectDirectory
    {

    }
}
