using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace numBlock
{
    class StoredInfo
    {
        private static String _rootDirectory = null;

        private static String getRoot() {
            if(_rootDirectory == null)
            {
                _rootDirectory = Path.Combine(
               Path.Combine(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).Parent.FullName, "Saved Games"),
               "numblock");
            }

            return _rootDirectory;
        }


        public static void SaveData(numblock data)
        {
            String root = getRoot();
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            XmlSerializer serializer = new XmlSerializer(typeof(numblock));
            TextWriter textWriter = new StreamWriter(getRoot() + "\\savedInfo.xml");
            serializer.Serialize(textWriter, data);

            textWriter.Close();
        }

        public static numblock LoadStoredData()
        {
            String root = getRoot();
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            numblock savedInfo = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(numblock));
                TextReader textReader = new StreamReader(getRoot() + "\\savedInfo.xml");

                savedInfo = (numblock)serializer.Deserialize(textReader);
                
                //backwards compatability
                if (savedInfo.playerinfo.combo == null)
                    savedInfo.playerinfo.combo = AchievementHub.MIN_COMBO_RECORD.ToString();

            } catch(Exception) {
                savedInfo = generateDefaultSaveInfo();
            }
            return savedInfo;
        }

        private static numblock generateDefaultSaveInfo()
        {
            numblock defaultData = new numblock();
            playerinfo playerinfo = new playerinfo();
            playerinfo.initials = "AAA";
            playerinfo.combo = AchievementHub.MIN_COMBO_RECORD.ToString();

            playerinfo.achievments = new achievment[]{};
            defaultData.playerinfo = playerinfo;

            defaultData.highscores = new highscores();
            defaultData.highscores.friends = new score[]{};
            defaultData.highscores.local = new score[]{};

            return defaultData;
        }
        
    }
}
