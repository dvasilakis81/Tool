using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace FilesTool
{


    class Program
    {
        private static string sourceDirectory;
        private static string destinationDirectory;
        private static bool setCreationTime;
        private static bool setLastAccessTime;
        private static bool setLastWriteTime;

        private static DateTime dateModifiedParam;

        private static int numOfFiles = 0;
        private static char charToSplit = '%';

        private static int dirsFixed = 0; // parent
        private static int dirsRead = 0; // parent

        private static int filesFixed = 0;
        private static int filesRead = 0;

        private static int filesNotFound = 0;
        private static int filesIgnored = 0;

        private static int unauthorizedExceptions = 0;
        private static int exceptions = 0;

        private static FileInfo unauthorizedExceptionsFileInfo;
        private static FileInfo exceptionsFileInfo;
        private static FileInfo filesNotFoundInfo;
        private static FileInfo filesIgnoredFileInfo;
        private static FileInfo compareDatetimeFileInfo;

        static void Main(string[] args)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("App started");
                Console.CursorVisible = false;

                // --------------------------------------------------------------------------------------------------------------------------------------
                FileInfo fi = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                unauthorizedExceptionsFileInfo = new FileInfo(Path.Combine(fi.DirectoryName, "UnauthorizedExceptions.txt"));
                if (unauthorizedExceptionsFileInfo.Exists)
                    unauthorizedExceptionsFileInfo.Delete();

                exceptionsFileInfo = new FileInfo(Path.Combine(fi.DirectoryName, "exceptions.txt"));
                if (exceptionsFileInfo.Exists)
                    exceptionsFileInfo.Delete();

                filesNotFoundInfo = new FileInfo(Path.Combine(fi.DirectoryName, "filesnotfound.txt"));
                if (filesNotFoundInfo.Exists)
                    filesNotFoundInfo.Delete();

                filesIgnoredFileInfo = new FileInfo(Path.Combine(fi.DirectoryName, "filesignored.txt"));
                if (filesIgnoredFileInfo.Exists)
                    filesIgnoredFileInfo.Delete();

                compareDatetimeFileInfo = new FileInfo(Path.Combine(fi.DirectoryName, "comparedatetimes.txt"));
                if (compareDatetimeFileInfo.Exists)
                    compareDatetimeFileInfo.Delete();                

                //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                string[] items = new string[2];

                for (int i = 0; i < args.Length; i++)
                {
                    int indexOfChar = args[i].IndexOf(charToSplit);
                    if (indexOfChar <= 0)
                    {
                        Console.WriteLine(string.Format("ERROR: Input parameter char {0} missing!", charToSplit));
                        Console.WriteLine("Press a key to continue!");
                        Console.ReadKey();
                        System.Environment.Exit(0);
                    }

                    items[0] = args[i].Substring(0, indexOfChar);
                    items[1] = args[i].Substring(indexOfChar + 1);

                    if (string.Compare(items[0], "source") == 0)
                    {
                        sourceDirectory = items[1];
                        if (sourceDirectory.StartsWith("\"") && sourceDirectory.EndsWith("\""))
                            sourceDirectory = sourceDirectory.Substring(1, sourceDirectory.Length - 2);
                    }
                    else if (string.Compare(items[0], "destination") == 0)
                    {
                        destinationDirectory = items[1];
                        if (destinationDirectory.StartsWith("\"") && destinationDirectory.EndsWith("\""))
                            destinationDirectory = destinationDirectory.Substring(1, destinationDirectory.Length - 2);
                    }
                    else if (string.Compare(items[0], "setCreationTime") == 0)
                        setCreationTime = (string.Compare(items[1], "true") == 0 ? true : false);
                    else if (string.Compare(items[0], "setLastAccessTime") == 0)
                        setLastAccessTime = (string.Compare(items[1], "true") == 0 ? true : false);
                    else if (string.Compare(items[0], "setLastWriteTime") == 0)
                        setLastWriteTime = (string.Compare(items[1], "true") == 0 ? true : false);
                    else if (string.Compare(items[0], "DateModified") == 0)
                    {
                        var cultureInfo = new CultureInfo("el-GR");
                        dateModifiedParam = DateTime.ParseExact(items[1], "dd/MM/yyyy", cultureInfo);
                    }
                }

                if (string.IsNullOrEmpty(sourceDirectory))                
                {
                    Console.WriteLine("ERROR: parameter 'source must have a value!");
                    Console.WriteLine("Press a key to continue!");
                    Console.ReadKey();
                    System.Environment.Exit(0);
                }

                if (string.IsNullOrEmpty(destinationDirectory))
                {
                    Console.WriteLine("ERROR: parameter destination must have a value!");
                    Console.WriteLine("Press a key to continue!");
                    Console.ReadKey();
                    System.Environment.Exit(0);
                }

                if (setCreationTime == false && setLastAccessTime == false && setLastWriteTime == false)
                {
                    Console.WriteLine("ERROR: At least one of the setCreationtime|setLastAccessTime|setLastWriteTime must be true!");
                    Console.WriteLine("Press a key to continue!");
                    Console.ReadKey();
                    System.Environment.Exit(0);
                }


                //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                FixDirectories();

                Console.WriteLine("");
                Console.WriteLine("*******************************************************************************");
                Console.WriteLine("*******                      C-O-M-P-L-E-T-E-D!                         *******");
                Console.WriteLine("*******************************************************************************");

                Console.WriteLine("Press a key to continue!");
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }

        //public static List<string> GetDirectories(string path, string searchPattern, SearchOption searchOption)
        //{
        //    //if (searchOption == SearchOption.TopDirectoryOnly)
        //    //    return Directory.GetDirectories(path, searchPattern).ToList();

        //    List<string> directories = new List<string>(GetDirectories(path, searchPattern));
        //    for (var i = 0; i < directories.Count; i++)
        //        directories.AddRange(GetDirectories(directories[i], searchPattern));

        //    return directories;
        //}
        //private static List<string> GetDirectories(string path, string searchPattern)
        //{
        //    try
        //    {
        //        return Directory.GetDirectories(path, searchPattern).ToList();
        //    }
        //    catch (Exception ex)
        //    {                
        //        return new List<string>();
        //    }
        //}

        private static void FixFileDates(string dir, Stopwatch stopwatch)
        {
            //int i = 0;
            //int percent = 0;
            // i = i + 1;
            //percent = (int)Math.Floor(((float)i / directories.Count) * 100);
            //ConsoleUtility.WriteProgressBar(percent, true);

            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                List<string> directoryFiles = null;
                if (di.Exists)
                    directoryFiles = new List<string>(Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly));

                if (directoryFiles != null && directoryFiles.Count > 0)
                {
                    //Fix directory
                    try
                    {
                        string temp1 = di.FullName.Substring(di.FullName.IndexOf(sourceDirectory) + sourceDirectory.Length);
                        if (temp1.StartsWith("\\"))
                            temp1 = temp1.Substring(1);
                        string destDirectoryToUpdate = Path.Combine(destinationDirectory, temp1);
                        DirectoryInfo destDirectoryToUpdateFi = new DirectoryInfo(destDirectoryToUpdate);
                        if (destDirectoryToUpdateFi.Exists)
                        {

                            if (setLastWriteTime)
                            {
                                DateTime destDtModified = Directory.GetLastWriteTime(destDirectoryToUpdateFi.FullName);
                                if (destDtModified.Date.CompareTo(dateModifiedParam.Date) == 0)
                                {
                                    Directory.SetLastWriteTime(destDirectoryToUpdateFi.FullName, Directory.GetLastWriteTime(di.FullName));
                                    Directory.SetLastWriteTimeUtc(destDirectoryToUpdateFi.FullName, Directory.GetLastWriteTimeUtc(di.FullName));
                                    dirsFixed += 1;
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        unauthorizedExceptions += 1;
                        SaveInfo(unauthorizedExceptionsFileInfo, ex.Message);
                        //Trace.WriteLine("Write Directory Exception: " + ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        exceptions += 1;
                        SaveInfo(exceptionsFileInfo, ex.Message);
                        //Trace.WriteLine("Write Directory Exception: " + ex.ToString());
                    }

                    //Fix directory
                    foreach (string sourceFilePath in directoryFiles)
                    {
                        try
                        {
                            string temp = sourceFilePath.Substring(sourceFilePath.IndexOf(sourceDirectory) + sourceDirectory.Length);
                            if (temp.StartsWith("\\"))
                                temp = temp.Substring(1);
                            if (destinationDirectory.EndsWith("\\"))
                                destinationDirectory = destinationDirectory.Substring(0, destinationDirectory.Length - 1);

                            string destinationFilePath = Path.Combine(destinationDirectory, temp);
                            FileInfo destFileInfo = new FileInfo(destinationFilePath);

                            filesRead += 1;
                            if (destFileInfo.Exists)
                            {
                                DateTime sourceDtModified = Directory.GetLastWriteTime(sourceFilePath);
                                DateTime destDtModified = Directory.GetLastWriteTime(destFileInfo.FullName);
                                
                                if (sourceDtModified.Date.CompareTo(destDtModified.Date) > 0) {
                                    filesIgnored += 1;
                                    SaveInfo(filesIgnoredFileInfo, string.Format("File {0} has datemodified greater than {1}", sourceFilePath, destFileInfo.FullName));
                                } else if (destDtModified.Date.CompareTo(dateModifiedParam.Date) == 0)
                                {
                                    File.SetLastWriteTime(destFileInfo.FullName, sourceDtModified);
                                    File.SetLastWriteTimeUtc(destFileInfo.FullName, File.GetLastWriteTimeUtc(sourceFilePath));
                                    filesFixed += 1;
                                } 
                                else
                                {
                                    //SaveInfo(compareDatetimeFileInfo, string.Format("Source     :{0}", sourceDtModified, destDtModified));
                                    //SaveInfo(compareDatetimeFileInfo, string.Format("Destination:{1}\n", sourceDtModified, destDtModified));
                                }

                                filesRead += 1;
                            }
                            else
                            {
                                filesNotFound += 1;
                                SaveInfo(filesNotFoundInfo, string.Format("File {0} not found", destFileInfo.FullName));
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            unauthorizedExceptions += 1;
                            SaveInfo(unauthorizedExceptionsFileInfo, ex.Message);
                            //Trace.WriteLine("Write Directory Exception: " + ex.ToString());
                        }
                        catch (Exception ex)
                        {
                            exceptions += 1;
                            SaveInfo(exceptionsFileInfo, ex.Message);
                            //Trace.WriteLine("Write File Exception: " + ex.ToString());
                        }
                    }

                    StringBuilder sb = new StringBuilder();

                    //sb.AppendLine(string.Format("Time elapsed {0} \n", stopwatch.Elapsed.ToString("hh\\:mm\\:ss")));
                    sb.AppendLine(string.Format("Time elapsed {0} \n", stopwatch.Elapsed));
                    sb.AppendLine(string.Format("Source            : {0}", sourceDirectory));
                    sb.AppendLine(string.Format("Destination       : {0}", destinationDirectory));

                    sb.AppendLine(string.Format("SetLastWriteTime  : {0}", setLastWriteTime));
                    sb.AppendLine(string.Format("DateModified To Fix  : {0}", dateModifiedParam.Date.ToString("dd MMM yyyy")));

                    sb.AppendLine("");
                    sb.AppendLine(string.Format("Directories   (fixed/read)   : {0}/{1}", dirsFixed, dirsRead));
                    sb.AppendLine(string.Format("Files         (fixed/read)   : {0}/{1}", filesFixed, filesRead));
                    sb.AppendLine(string.Format("Files Not Found (fixed/read) : {0} (More info at filesnotfound.txt)", filesNotFound));
                    sb.AppendLine(string.Format("Files Ignored                : {0} (More info at filesignored.txt)\n", filesIgnored));

                    sb.AppendLine(string.Format("UnauthorizedExceptions : {0} (More info at UnauthorizedExceptions.txt)", unauthorizedExceptions));
                    sb.AppendLine(string.Format("Exceptions             : {0} (More info at Exceptions.txt)", exceptions));

                    //sb.AppendLine((string.Format("Directories {0}\nFiles {1}\nFilesNotFound {2}\nUnauthorizedExceptions {3} (More info at UnauthorizedExceptions.txt)\nExceptions {4} (More info at Exceptions.txt)", 
                    //    dirsFixed, filesFixed, filesNotFound, unauthorizedExceptions, exceptions)));

                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(sb.ToString());
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                unauthorizedExceptions += 1;
                SaveInfo(unauthorizedExceptionsFileInfo, ex.Message);
            }
            catch (Exception ex)
            {
                exceptions += 1;
                SaveInfo(exceptionsFileInfo, ex.Message);
            }
        }

        private static void FixDirectories()
        {

            List<string> dirsToAdd = new List<string>();
            List<string> dirsToRemove = new List<string>();

            List<string> dirs = new List<string>();
            dirs.AddRange(Directory.GetDirectories(sourceDirectory));
            int numOfDirs = 1;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //Fix file dates for root folder
            FixFileDates(sourceDirectory, stopwatch);

            do
            {
                dirsToAdd = new List<string>();
                dirsToRemove = new List<string>();               

                //Fix file dates
                foreach (string dir in dirs)
                {
                    FixFileDates(dir, stopwatch);
                    dirsRead += 1;
                }

                dirsToRemove.AddRange(dirs);
                foreach (string dir in dirs)
                {

                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        if (di.Exists)
                            dirsToAdd.AddRange(Directory.GetDirectories(dir));
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        unauthorizedExceptions += 1;
                        SaveInfo(unauthorizedExceptionsFileInfo, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        exceptions += 1;
                        SaveInfo(exceptionsFileInfo, ex.Message);
                    }
                }
                numOfDirs += dirsToAdd.Count;
                //Remove fixed dirs from stack
                foreach (string dirToRemove in dirsToRemove)
                {
                    if (dirs.Contains(dirToRemove))
                        dirs.Remove(dirToRemove);
                }

                //Add new fixed dirs from stack
                dirs.AddRange(dirsToAdd);
            } while (dirs != null && dirs.Count > 0);

            Trace.WriteLine("# directories " + numOfDirs);
            Trace.WriteLine("# files " + numOfFiles);
        }
        private static void SaveInfo(FileInfo fi, string content)
        {
            File.AppendAllText(fi.FullName, content + '\n');
        }
    }
}


// numOfDirectories = 1;
// List<string> directories = GetDirectories(directory1, "*", SearchOption.AllDirectories);
// int i = 0;
// int percent = 0;
// ConsoleUtility.WriteProgressBar(0);
// foreach (string directory in directories)
// {
//    i = i + 1;
//    percent = (int)Math.Floor(((float)i / directories.Count) * 100);
//    ConsoleUtility.WriteProgressBar(percent, true);
//    var directoryFiles = new List<string>(Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly));
//    foreach (string filePath in directoryFiles)
//    {
//        try
//        {
//            string temp = filePath.Substring(filePath.IndexOf(args[0]) + args[0].Length + 1);
//            string pathOfFileToUpdateInfo = Path.Combine(directory2, temp);

//            File.SetCreationTime(pathOfFileToUpdateInfo, File.GetCreationTime(filePath));
//            File.SetCreationTimeUtc(pathOfFileToUpdateInfo, File.GetCreationTimeUtc(filePath));

//            File.SetLastAccessTime(pathOfFileToUpdateInfo, File.GetLastAccessTime(filePath));
//            File.SetLastAccessTimeUtc(pathOfFileToUpdateInfo, File.GetLastAccessTimeUtc(filePath));

//            File.SetLastWriteTime(pathOfFileToUpdateInfo, File.GetLastWriteTime(filePath));
//            File.SetLastWriteTimeUtc(pathOfFileToUpdateInfo, File.GetLastWriteTimeUtc(filePath));

//            Trace.WriteLine("Success: File time info " + pathOfFileToUpdateInfo + " has been updated!");
//        }
//        catch (Exception ex)
//        {
//            Trace.WriteLine("Exception: " + ex.ToString());
//        }
//    }
// }