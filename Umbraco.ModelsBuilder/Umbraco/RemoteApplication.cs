//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Umbraco.ModelsBuilder.Umbraco
//{
//    public class RemoteApplication : MarshalByRefObject
//    {
//        public IList<TypeModel> GetContentAndMediaTypes(string connectionString, string databaseProvider)
//        {
//            IList<TypeModel> modelTypes;
//            using (var umbraco = Application.GetApplication(connectionString, databaseProvider, true))
//            {
//                modelTypes = umbraco.GetContentAndMediaTypes();
//            }
//            return modelTypes;
//        }
//    }
//}
