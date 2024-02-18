using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiZhenMySQL
{
    public class SubsidiaryAggregate<MasterT, ElementT> : DBObjContainer<ElementT> where MasterT : IDbObject where ElementT : IDbObject
    {
        public MasterT Master { get; }

        public virtual int LoadElement()
        {
            return LoadFromDB_SourceObj(Master);
        }
        public virtual int InsertElements(IEnumerable<ElementT> elements)
        {
            return DataBase.InsertToDB(elements);
        }
        public virtual int RemoveElements(IEnumerable<ElementT> elements)
        {
            int re = 0;
            foreach (var item in elements)
            {
                re += DataBase.DeleteFromDB(item);
            }
            return re;
        }

        public virtual int LoadElement_Chain(int mark = 0)
        {
            return LoadFromDB_Chain(Master, mark);
        }
        public virtual int InsertElements_Chain(IEnumerable<ElementT> elements, int mark = 0)
        {
            return DataBase.InsertToDB_Chains(Master, elements, mark);
        }
        public virtual int RemoveElements_Chain(IEnumerable<ElementT> elements)
        {
            return DataBase.DeleteFromDB_Chains(Master, elements);
        }

        public SubsidiaryAggregate(MasterT master)
        {
            Master = master;
        }

    }
}
