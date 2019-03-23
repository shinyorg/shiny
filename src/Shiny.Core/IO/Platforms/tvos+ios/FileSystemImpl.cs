using System;
using System.IO;
using System.Linq;
using Foundation;


namespace Shiny.IO
{
    public class FileSystemImpl : IFileSystem
    {
        public FileSystemImpl()
        {
            this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
            this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
            this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
        }


        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }


        public string ToFileUri(string path) => path;


        static DirectoryInfo ToDirectory(NSSearchPathDirectory dir)
            => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());
    }
}
/*
AppName.app

This is the app’s bundle. This directory contains the app and all of its resources.

You cannot write to this directory. To prevent tampering, the bundle directory is signed at installation time. Writing to this directory changes the signature and prevents your app from launching. You can, however, gain read-only access to any resources stored in the apps bundle. For more information, see the Resource Programming Guide

The contents of this directory are not backed up by iTunes or iCloud. However, iTunes does perform an initial sync of any apps purchased from the App Store.

Documents/

Use this directory to store user-generated content. The contents of this directory can be made available to the user through file sharing; therefore, his directory should only contain files that you may wish to expose to the user.

The contents of this directory are backed up by iTunes and iCloud.

Documents/Inbox

Use this directory to access files that your app was asked to open by outside entities. Specifically, the Mail program places email attachments associated with your app in this directory. Document interaction controllers may also place files in it.

Your app can read and delete files in this directory but cannot create new files or write to existing files. If the user tries to edit a file in this directory, your app must silently move it out of the directory before making any changes.

The contents of this directory are backed up by iTunes and iCloud.

Library/

This is the top-level directory for any files that are not user data files. You typically put files in one of several standard subdirectories. iOS apps commonly use the Application Support and Caches subdirectories; however, you can create custom subdirectories.

Use the Library subdirectories for any files you don’t want exposed to the user. Your app should not use these directories for user data files.

The contents of the Library directory (with the exception of the Caches subdirectory) are backed up by iTunes and iCloud.

For additional information about the Library directory and its commonly used subdirectories, see The Library Directory Stores App-Specific Files.

tmp/

Use this directory to write temporary files that do not need to persist between launches of your app. Your app should remove files from this directory when they are no longer needed; however, the system may purge this directory when your app is not running.

The contents of this directory are not backed up by iTunes or iCloud.
 */