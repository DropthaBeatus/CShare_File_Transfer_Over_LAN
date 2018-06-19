using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace PushingforImaging{
    /* I left some global variables to let you edit some things in the software
     * ONLY EDIT THE ONES THAT CONTAIN INFORMATION (Already establish) NOT THE ONES THAT ARE NULL
     ALSO ALL CAPS = IMPORTANT INFO*/
    class Program
    {
        //DO NOT EDIT THESE BELOW UNLESS YOU KNOW WHAT YOU ARE DOING

        public static string cmpnm;
        //contains all the paths for the folders. The programs creates all the folders first then AFTER EVERY FOLDER IS CREATED the program will insert the files
        public static List<string> folderpavements = new List<string>();
        //This program stores the paths for all the files
        public static List<string> files = new List<string>();
        public static string username;
        //This is a class that stores all information such as:
        /*  UserName
         * ComputerName
         * Files that didn't transfer correctly 
         * and premade extensions that filter out stuff for folder paths It is more explained in the FIlePaths.cs file
         * */
        public static FilePaths Users;
        //This variable is the pathing to create the main file DO NOT EDIT IT;
       // public static string depotPath = @"C:\Users\liam.flaherty\";
        public static string firstpath;
        public static BasePath DepotPath = new BasePath(@"c:\");

        //YOU CAN EDIT THESE VARIABLES HERE BELOW

        //This variable determines who is an active user last activity time by months. You can edit here. 
        public static int USERKILLTIME = -3;
        //These two ints are used to filter out old files ModKillTime is used for the last time the file was modified and the CreatedKill is for when the file was created
        public static int ModKillTime = -2;
        public static int CreatedKill = -4;
        //This two numbers below are the maxsize in bytes of a file/folder that will ask the user if they want to transfer a file that big
        public static float MAXfolderSIZE = 100000000;
        public static float MAXfileSIZE = 10000000;
        //This array is a filter for extensions Most of these extensions are for hazardous files or shortcuts
        //If you want to add more extensions just change the default number at new string[default number] to how many extnsions there are and put your filter as "filterName", in the array below
        public static string[] securityArr = new string[16] { ".sample", ".resx", ".ide", ".cache", "lref", ".baml", ".config", ".cs", ".csproj", ".ami", ".ini", ".url",".htm", ".exe", ".dll",".maf" };

        public static ComboBox comboInstalledPrinters = new ComboBox();
        public static PrintDocument printDoc = new PrintDocument();

        static void Main(string[] args)
        {
            bool i = true;
            while (i == true)
            {
                Menu();
                Console.WriteLine("Transfer files for more users? Hit 1 to continue:");
                string k = Console.ReadLine();
                if (k != "1")
                    i = false;
            }
            //fix a way to prevent console closing and what not instead of sleep it
            Thread.Sleep(50000);
        }

        public static void Menu()
        {
            Console.WriteLine("Enter name of the computer");
            cmpnm = @"\\" + Console.ReadLine() + @"\c$";
            while(Login(cmpnm) == false)
            {
                Console.WriteLine("Enter name of the computer");
                cmpnm = @"\\" + Console.ReadLine() + @"\c$";
            }
            //firstpath is just \\ComputerName\c$\Users 
            firstpath = GetUsernames(cmpnm + @"\Users");
            Console.WriteLine(firstpath);
            username = Path.GetFileName(firstpath);
            Users = new FilePaths(username, firstpath, DepotPath.depotPath);
            while (Login(firstpath) == false)
            {
                Console.WriteLine("Enter name of the computer");
                cmpnm = @"\\" + Console.ReadLine() + @"\c$";
                firstpath = GetUsernames(cmpnm + @"\Users");
                Users = new FilePaths(username, firstpath, DepotPath.depotPath);
            }
            CreateDir();
            CheckChrome();
            //will need a way to have user break this and list all bad file paths and what not 
            try
            {
                int countBadFiles = 0;
                while (Users.badFiles1.Count > 0 && Users.badFiles2.Count > 0 || countBadFiles > 5)
                {
                    if (Users.badFiles1.Count > 0)
                    {
                        foreach (string[] s in Users.badFiles1)
                        {
                            FileCompress(Path.GetFileName(s[0]), s[0], s[1]);
                        }
                        Users.badFiles1.Clear();
                    }
                    if (Users.badFiles2.Count > 0)
                    {
                        foreach (string[] s in Users.badFiles2)
                        {
                            FileCompress(Path.GetFileName(s[0]), s[0], s[1]);

                        }
                        Users.badFiles2.Clear();
                    }
                    countBadFiles++;
                }
            }
            catch (NullReferenceException) { }

        }

        //need to use this to prevent files from being transfered might need to store an array or list and do an  S.contains() field 
        //https://www.dotnetperls.com/directory-size
        //reference here 
        //This program reads the size of the file 
        //If it is big enough it gives the size and ask the user if they want to transfer it
        public static bool GetFileSize(string test)
        {
            bool transfer = true;
            string check;

            long b = 0;
            FileInfo info = new FileInfo(test);
            b = info.Length;


            if (b > MAXfileSIZE)
            {
                string temp = System.IO.Path.GetFileName(test);
                Console.ForegroundColor = ConsoleColor.Green;
                b = b / 1000;
                if(b > 1000)
                {
                    b = b / 1000;
                    Console.WriteLine("FILE size{0} at maximum is: {1} Mb would you like to transfer it?\nHIT 1 TO CANCEL TRANSFER", temp, b);
                }
                else
                {
                    Console.WriteLine("FILE size{0} at maximum is: {1} kB would you like to transfer it?\nHIT 1 TO CANCEL TRANSFER", temp, b);
                }

                Console.ResetColor();
                try
                {
                    check = Console.ReadLine();
                }
                catch
                {
                    check = "";
                }
                if (check == "1")
                    transfer = false;

            }
            return transfer;
        }
        //This program reads the size of a folder
        //If it is big enough it gives the size and ask the user if they want to transfer it
        public static bool GetFolderSize(string test)
        {
            bool transfer = true;
            string check;

            long FolderSize = 0;
            if (!Directory.EnumerateFileSystemEntries(test).Any())
            {
                return false;
            }
            foreach( string s in Directory.GetFiles(test,"*", SearchOption.AllDirectories))
            {
                if (FileFilter(s) == true)
                {
                    FileInfo info = new FileInfo(s);
                    FolderSize += info.Length;
                }
                if (FolderSize == 0)
                    return false;
            }
            if (FolderSize > MAXfolderSIZE)
            {
              
                string temp = System.IO.Path.GetFileName(test);
                Console.ForegroundColor = ConsoleColor.Blue;
                FolderSize = FolderSize / 1000;
                if (FolderSize > 1000)
                {
                    FolderSize = FolderSize / 1000;
                    Console.WriteLine("FOLDER size{0} at maximum is: {1} Mb would you like to transfer it?\nHIT 1 TO CANCEL TRANSFER", temp, FolderSize);
                }
                else
                Console.WriteLine("FOLDER {0} size is {1} kB would you like to transfer it?\nHIT 1 TO CANCEL TRANSFER", temp, FolderSize);
                Console.ResetColor();
                check = Console.ReadLine();
                if (check == "1")
                    transfer = false;
            }
            return transfer;
        }

        //This method is used to make sure the path exsist before the program continues 
        public static bool Login(string path)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            try
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("File path is not working. Either: Computer is incorrect, You Don't have access, or the target Computer is turned off");
                    Console.ResetColor();
                    return false;
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
                return false;
            }
                Console.ResetColor();
                return true;
        }

        //This program creates the main directory from depotpath and the 
        public static void CreateDir()
        {

            string path = firstpath;

            DateTime localT = DateTime.Now;
            string Timestamp = localT.ToString();
            //You replace these characters to prevent any errors\confusion in the directory pathing 
            Timestamp = Timestamp.Replace('/', '-').Replace(':', '.');
            string xx = path + @"\Favorites";
            string yy = path + @"\Desktop";
            string zz = path + @"\Documents";

            //If you understand the pathing here you can switch it out for whatever like to anypart of your C:\, \\depot\Data\Users\+username, or ITVault 
            //just make sure to add another path like \username+Timestamp the create the directory to store the files in
            
            //creates the directory to store all the files in

            System.IO.Directory.CreateDirectory(Users.depotPath);
            Users.depotPath = Users.depotPath + username + " " + Timestamp;//@"\\depot\Data\Users\" + username;


            string x = Users.depotPath + @"\Favorites-" + Timestamp;
            string y = Users.depotPath + @"\Desktop-" + Timestamp;
            string z = Users.depotPath + @"\Documents-" + Timestamp;

            try
            {
                if (Directory.GetFiles(xx).Length != 0)
                {
                    System.IO.Directory.CreateDirectory(x);
                    Console.WriteLine("Creating Folder:" + x);
                    TestingFolderCopy2(xx, x);
                    folderpavements.Clear();
                    files.Clear();
                }
                if (Directory.GetFiles(yy).Length != 0)
                {
                    System.IO.Directory.CreateDirectory(y);
                    Console.WriteLine("Creating Folder:" + y);
                    TestingFolderCopy2(yy, y);
                    folderpavements.Clear();
                    files.Clear();
                }
                if (Directory.GetFiles(zz).Length != 0)
                {
                    System.IO.Directory.CreateDirectory(z);
                    Console.WriteLine("Creating Folder:" + z);
                    TestingFolderCopy2(zz, z);
                    folderpavements.Clear();
                    files.Clear();

                }
                GetPrinterNames(Users.depotPath);
            }
             
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Your Access was denied");
                Menu();
            }
            
        }

        public static void TestingFolderCopy2(string SourcePath, string DestinationPath)
        {

             files = DirSearch(SourcePath);
            foreach (string s in folderpavements)
            {
                Console.WriteLine(s);
                FileAttributes attrib = File.GetAttributes(s);
                if ((attrib & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    try
                    {
                        Directory.CreateDirectory(s.Replace(SourcePath, DestinationPath));
                    }
                    catch
                    {
                        //Needs an actual fix
                        //Demlion seems like crap so I would not recommend it 
                        Console.WriteLine("How did you even get a path that long?");
                    }
                }
            }
            foreach (string s in files)
            {
                try
                {    
                        FileAttributes attrib = File.GetAttributes(s);
                        if ((attrib & FileAttributes.Directory) != FileAttributes.Directory)
                        {
                            Console.WriteLine("Copying File" + s);
                            Login(SourcePath);
                            FileCompress(s, SourcePath, DestinationPath);
                        }
                }
                catch (Exception ex)
                {
                    if (ex is IOException)
                    {
                        Users.badFiles1.Add(new string[] { SourcePath, DestinationPath });
                    }
                    Debug.Write(ex);
                }
            }
        }
        //This function checks each file extenstion and checks to see if it contains the same extension in the array that contains denied extensions
        //If the file does contain any of the extensions it is not copied over
        static bool FileFilter(string path)
        {
            for (int i = 0; i < securityArr.Length; i++)
            {
                if (path.Contains(securityArr[i]))
                {
                    return false;
                }
            }
            return true;
        }

        //recursive search folders and files and skips the ones with unauthorized access
        //You can not use any library functions in c# as it seems that if it runs in a unauthorized file in the middle of a search it will cancel the whole search 
        static List<string> DirSearch(string sDir)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    string d = sDir;
                    FileAttributes attrib = File.GetAttributes(d);
                    if ((attrib & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        folderpavements.Add(d);
                        Console.WriteLine("Directory added :" + d);
                    }
                    //might need to switch this to the above part here
                    Console.WriteLine("In Directory path");
                    if (FileFilter(f) == true && GetFileSize(f) == true)
                    {
                        if(TimePurge(f) == true && !f.Contains(@"\AppData\Local\Google\Chrome\User Data\Default"))
                        Console.WriteLine("Passed filter");
                        files.Add(f);
                    }
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    if (GetFolderSize(d) == true)
                    {
                         DirSearch(d);
                    }
                }       
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine("except Message:" + excpt.Message);
            }
            return files;
        }

        //This is a time filter and will not transfer files with an outdated Modified by and Created by date time
        //The variable ModTimeKill will be avialible at the top to change it goes by Years but you can easily change it to years etc (This is used for the file's Modified time date)
        //The same goes to CreatedKill for the Created time date
        //BOTH time restaints have to be met to kill the file
        public static bool TimePurge(string path)
        {
            DateTime fileTime = File.GetCreationTime(path);
            DateTime fileModTime = File.GetLastWriteTime(path);
            DateTime purgeTime = DateTime.Now;
            purgeTime = purgeTime.AddYears(CreatedKill);
            DateTime ModKill = DateTime.Now.AddYears(ModKillTime);
            int b = DateTime.Compare(purgeTime, fileTime);
            if (DateTime.Compare(purgeTime, fileTime) < 0 &&  DateTime.Compare(ModKill, fileModTime)< 0)
            {
                return false;
            }
            else
                return true;
        }


        //This is probably the most unorganized part of the code
        //This Compresses, Copies, Transfers, Compares the compressed source and transfer files, Uncompresses the transfered file, then Deletes the compressed files (Its overcomplicated I know I'll try to simplify it)
        public static void FileCompress(string fileP, string SourcePath, string DestinationPath)
        {

            int count = 0;

            if (fileP.Contains("Desktop"))
            {
                count = 1;
            }
            if (fileP.Contains("Favorites"))
            {
                count = 0;
            }
            if (fileP.Contains("Documents"))
            {
                count = 2;
            }
            if (fileP.Contains(@"\AppData\Local\Google\Chrome\User Data\Default"))
            {
                count = 3;
            }
            //compressing an already compressed file causes an error
            if (!fileP.Contains(".gz")){
                string zipPath;
                //This part here is well... confusing to explain but it takes a huge chunk of the source path out including the file name
                //E.g. \\CmpName\User\username\Documents\folder1\folder2\Textfile.txt ---> \folder1\folder2\ then addes this the premade directory as PremadeDirectoryPath\Documents-Timestamp\folder1\folder2\ then inserts the text file in this location
                //This is so the files get put in the same structure (directories) as the source files were organized
                FileInfo dir = new FileInfo(fileP);
                zipPath = Compress(dir);
                if(zipPath == "")
                {
                    return;
                }
                string tempName = zipPath.Replace(Users.foldernames[count], "");
                DestinationPath = DestinationPath + tempName;
                //need a catch for this portion right here
                File.Copy(zipPath, zipPath.Replace(zipPath, DestinationPath), true);

                //This checks the integrity of the file and sees if the file is a true copy
                //True = the files has its integrity
                //It then deletes both compresed files
                if (HashAndCompare(zipPath, DestinationPath) == true)
                {
                    File.Delete(zipPath);
                    FileInfo dest = new FileInfo(DestinationPath);
                    Decompress(dest);
                    File.Delete(DestinationPath);
                }
                else
                {
                    //This deletes the transfered file because it has no integrity including the compressed source folder
                    File.Delete(DestinationPath);
                    File.Delete(zipPath);
                }
            }
        }

        //Decompress the transfered file and then deletes both compressed files
        public static void Decompress(FileInfo file)
        {
           
            using(FileStream inFile = file.OpenRead())
            {
                string curFile = file.FullName;
                try
                {
                    string origName = curFile.Remove(curFile.Length - file.Extension.Length);

                    using (FileStream outFile = File.Create(origName))
                    {
                        using (GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress))
                        {
                            try
                            {
                                Decompress.CopyTo(outFile);
                            }
                            catch (InvalidDataException ex)
                            {
                                Debug.Write(ex);
                            }
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException ex)
                {
                    Debug.Write(ex);
                }
            }
        }

        //Compresses files as long as they have not already been compressed
        public static string Compress(FileInfo file)
        {
            try
            {
                using (FileStream inFile = file.OpenRead())
                {
                    //figure out .gz extension
                    if ((File.GetAttributes(file.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & file.Extension != ".gz")
                    {
                        using (FileStream outFile = File.Create(file.FullName + ".gz"))
                        {
                            using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                            {

                                inFile.CopyTo(Compress);
                                return outFile.Name;

                            }

                        }
                    }
                }
                return file.Name;
            }
            catch (System.IO.IOException ex)
            {
                Debug.Write(ex);
            }
            //need to probably fix this really quick

            return "";
            }
        //This method takes both compressed files hashes them and then compares them.
        //If they are an exact match in their hash code then its perfect and the transfer goes through (this method returns true)
        //If not the files are not transfered and the method returns false
        public static bool HashAndCompare(string sourcePath, string DestinationPath)
        {
            using(var md5 = MD5.Create())
            {
                sourcePath.Replace(@"\", "");
                try
                {
                    using (var stream1 = File.OpenRead(DestinationPath))
                    {
                        using (var stream2 = File.OpenRead(sourcePath))
                        {
                            var hash1 = md5.ComputeHash(stream1);
                            var hash2 = md5.ComputeHash(stream2);
                            string onePath = BitConverter.ToString(hash1).Replace("-", "").ToLowerInvariant();
                            string twoPath = BitConverter.ToString(hash2).Replace("-", "").ToLowerInvariant();
                            if (onePath.Equals(twoPath))
                            {
                                return true;
                            }
                            else
                            {
                                if (Users.badFiles1.Count > 0)
                                {
                                    Users.badFiles2.Add(new string[] { onePath, twoPath });
                                }
                                else if (Users.badFiles2.Count > 0)
                                {
                                    Users.badFiles1.Add(new string[] { onePath, twoPath });
                                   
                                }
                                    return false;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Thread.Sleep(50000);
                }
            }
            return false;

        }


        //This function takes the firstpath variable (\\CmpName\c$\Users) and search the top directories for created users
        //The function will print out the users that have had activity in the past 3 months
        //It does that by finding the \NTUSER.DAT file in the user pathing (\\CmpName\c$\Users\username\NTUSER.DAT)
        //It checks the last time the NTUSER.DAT file was modified and then determines if its an active user
        //You can change the time determiniation by editing   if (DateTime.Compare(DateTime.Now.AddMonths(-3), temp)<= 0 ) line
        //Just remove the .AddMonths(number) part and insert .AddDays, AddYears, ETC
        //MAKE SURE THE NUMBER IS NEGATIVE 
        //I left a variable at the top so you can edit the number at the top
        public static string GetUsernames(string path)
        {
            List<string> AcceptableUser = new List<string>();
            Login(path);
            string[] users = Directory.GetDirectories(path);
            int max = users.Length;
            int chosen = -1;
            for (int y = 0; y < max; y++)
            {
                try
                {
                    DateTime temp = File.GetLastWriteTime(users[y] + @"\NTUSER.DAT");
                    if (DateTime.Compare(DateTime.Now.AddMonths(USERKILLTIME), temp)<= 0 )
                    {
                        AcceptableUser.Add(users[y]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                }
            }
            AcceptableUser.Add("IS THE DEPOT PATH PRESS THIS NUMBER TO CHANGE IT---> " + DepotPath.depotPath);
            //will need an if statement here if there are no users that hit a certain time limit
            while(chosen > AcceptableUser.Count || chosen < 1)
            {

                int pick = 0;
                foreach (string user in AcceptableUser)
                {
                    pick++;
                    Console.WriteLine(pick + "--->" + user);
                }
                Console.WriteLine("Which user would you like to select? Enter in the corresponding number. \nThese have been active in at least 3 months");
                try
                {
                    chosen = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    chosen = -1;
                }
                if (chosen == (pick))
                {
                    PathCustomization();
                    AcceptableUser.RemoveAt(AcceptableUser.Count - 1);
                    AcceptableUser.Add("IS THE DEPOT PATH PRESS THIS NUMBER TO CHANGE IT -->" + DepotPath.depotPath);
                    chosen = -1;
                }
            }
            return AcceptableUser[chosen-1];
            //have users select what users they would like to add I'll need a hecka ton catch statements here
            //need to figure out return types here my god this is going to get crazy intense
        }

        public static void CheckChrome()
        {
            string SourcePath = firstpath + @"\AppData\Local\Google\Chrome\User Data\Default";
            DateTime localT = DateTime.Now;
            string Timestamp = localT.ToString();
            //You replace these characters to prevent any errors\confusion in the directory pathing 
            Timestamp = Timestamp.Replace('/', '-').Replace(':', '.');
            string DestinationPath = Users.depotPath +  @"\GoogleChromeData - " + Timestamp;
      
            if (Login(SourcePath) == true)
            {
                    Console.WriteLine("Creating Folder: " + DestinationPath);
                     System.IO.Directory.CreateDirectory(DestinationPath);
                     TestingFolderCopy2(SourcePath, DestinationPath);
            }
            else
                Console.WriteLine("Cannot transfer chrome profile because the path " + SourcePath + " Does not exist");
            folderpavements.Clear();
            files.Clear();
        }

        public static void PathCustomization()
        {
            Console.WriteLine("What is the new depot path you'd like to select?");
            DepotPath.depotPath = Console.ReadLine();
            while (Login(DepotPath.depotPath) == false)
            {
                Console.WriteLine("Path incorrect. Please select a path in the Network Drive");
                DepotPath.depotPath = Console.ReadLine();
            }
            return;
       
        }

        //This function takes all printer names in the network and finds the default printer
        public static void GetPrinterNames(string path)
        {
            string DefPrinter = "";
             comboInstalledPrinters.Dock = DockStyle.Top;
            string PrintertxtContents = "";
            string pkInstalledPrinters;

            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                Console.WriteLine(printer);
                PrintertxtContents = PrintertxtContents + printer + "\n               ";
                pkInstalledPrinters = printer;
                comboInstalledPrinters.Items.Add(pkInstalledPrinters);
                if (printDoc.PrinterSettings.IsDefaultPrinter)
                {
                   DefPrinter = printDoc.PrinterSettings.PrinterName;
                   DefPrinter = Path.GetFileName(DefPrinter);
                }

            }
            if (DefPrinter != "")
            {
                System.IO.File.WriteAllText(path + @"\" + DefPrinter + @".txt", PrintertxtContents + "Main Printer is -" + DefPrinter);
            }
        }

    }
}
