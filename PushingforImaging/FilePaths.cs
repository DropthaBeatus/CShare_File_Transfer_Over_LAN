using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushingforImaging
{
    class FilePaths : BasePath
    {
        public string username;
        public string Path;
        //public List<string> foldernames = new List<string>();
        public string[] foldernames = new string[5];
        public bool[] filesInIt = new bool[3];
        // DateTime createTime;
        List<string> filenames = new List<string>();
        public string Name;
        public List<string[]> badFiles1 = new List<string[]>();
        public List<string[]> badFiles2 = new List<string[]>();
      
        public FilePaths(string name , string fullname, string path, string cmpnm):base(path)
        {
            Path = path;
            username = name;
            filesInIt[0] = false;
            filesInIt[1] = false;
            filesInIt[2] = false;

            Name = fullname;
            foldernames[0] = (Name + @"\Favorites");
            foldernames[2] = (Name + @"\Documents");
            foldernames[1] = (Name + @"\Desktop");
            foldernames[3] = (Name + @"\AppData\Local\Google\Chrome\User Data\Default");
            foldernames[4] = (cmpnm + @"\Data");
            //find file path for google chrome favorites
        }

        //I didn't use this at all but this a way to store all files that are not correct
        public void GetAllFiles(string path)
        {
            filenames.Add(path);
        }
    }
}
