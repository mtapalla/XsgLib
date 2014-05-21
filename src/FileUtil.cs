/*
 ****************************************************************************
 * Author: Marc Tapalla
 * Email: marc.tapalla@gmail.com
 *   
 * Library for SCPI remote control of Agilent's X-Series Signal Generators.
 * 
 * Instruments Supported:
 *  MXG (N5182A/N5182B)
 *  EXG (N5172B) *untested*
 *  Untested, but should be compatible with other XSG models 
 * 
 ****************************************************************************
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace XsgLib
{
    public static class FileUtil
    {
        public static string MergePath(string dir, string file)
        {
            bool unix = false;
            if (dir.Contains("\\") || file.Contains("\\"))
            {
                dir = dir.TrimEnd('\\');
                file = file.TrimStart('\\');
            }
            else
            {
                unix = true;
                dir = dir.TrimEnd('/');
                file = file.TrimStart('/');
            }
            return dir + (unix ? "/" : "\\") + file;
        }

        public static void CopyFile(string sourceFile, string TargetFile)
        {
            File.Copy(sourceFile, TargetFile, true);
        }

        public static void WriteFile(string path, string[] lines)
        {
            try
            {
                File.WriteAllLines(path, lines);
            }
            catch (FileNotFoundException e)
            {
                throw new Exception("Failed to write to file [" + path + "]", e);
            }
        }

        public static byte[] ReadBinaryFile(string path)
        {
            BinaryReader b = new BinaryReader(File.Open(path, FileMode.Open));
            int length = (int)b.BaseStream.Length;
            byte[] data = b.ReadBytes(length);
            b.Close();

            return data;
        }

        public static string[] ReadFile(string path)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(@path);
            }
            catch (FileNotFoundException e)
            {
                throw new Exception("Failed to open file [" + path + "] for reading", e);
            }
            return lines;
        }

        public static long GetFileSize(string path)
        {
            FileInfo f = new FileInfo(path);
            return f.Length;
        }

        public static string GetAbsolutePath(string relativePath)
        {
            relativePath = relativePath.TrimStart('\\');
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + relativePath;
        }

        public static bool FileExists(string absolutePath)
        {
            return File.Exists(absolutePath);
        }

        public static bool DirExists(string absolutePath)
        {
            return Directory.Exists(absolutePath);
        }

        public static void CreateDirectoryIfNotExist(string absolutePath)
        {
            if (!DirExists(absolutePath))
                Directory.CreateDirectory(absolutePath);
        }
    }
}
