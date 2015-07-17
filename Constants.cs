using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_EnOcean
{
    static public class Constants
    {
        public static String PLUGIN_STRING_ID = "HS3_EnOcean";
        public static String PLUGIN_STRING_NAME = "EnOcean plugin";
        public static String DEVICE_PLUGIN_ID_KEY = "hs3_enocean_id_key";
        public static String DEVICE_PLUGIN_VERSION_KEY = "hs3_enocean_revision_key";
        public static TimeSpan HOUSEKEEPER_RUN_INTERVAL = new TimeSpan(0, 5, 0);
      //  public static String DATABASE_DIRECTORY = "Data/" + Constants.PLUGIN_STRING_ID;
       // public static String DATABASE_NAME = Constants.PLUGIN_STRING_ID + ".db";
        public static String PLUGIN_EXSTRA_DATABASE_ID_KEY = "PLUGIN:" + Constants.PLUGIN_STRING_ID + ":DBID";
    }
}
