using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetsLibrary;


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaGalleryScannerImpl : IMediaGalleryScanner
    {
        public Task<IEnumerable<Media>> GetMediaSince(DateTimeOffset date)
        {
            //ALAssetsLibrary.Notifications.
            //ALAssetsLibrary.DisableSharedPhotoStreamsSupport
            //ALAssetsLibrary.ChangedNotification
            //ALAssetsLibrary.AuthorizationStatus
            //ALAssetsLibrary.DeletedAssetGroupsKey
            var library = new ALAssetsLibrary();
            //var del = new ALAssetsLibraryGroupsEnumerationResultsDelegate((group, ref bool value) => {
            //});
            //del.(group, success) =>
            //{

            //});

            //library.Enumerate(
            //    ALAssetsGroupType.Library,
            //    (sender, ref value) => { },
            //    e => { }
            //);
            var asset = new ALAsset();

            return null;
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}
/*
#import <UIKit/UIKit.h>
#include <AssetsLibrary/AssetsLibrary.h> 

@interface getPhotoLibViewController : UIViewController
{
 ALAssetsLibrary *library;
 NSArray *imageArray;
 NSMutableArray *mutableArray;
}

-(void)allPhotosCollected:(NSArray*)imgArray;

 @end 
 


static int count=0;

@implementation getPhotoLibViewController

-(void)getAllPictures
{
 imageArray=[[NSArray alloc] init];
 mutableArray =[[NSMutableArray alloc]init];
 NSMutableArray* assetURLDictionaries = [[NSMutableArray alloc] init];

 library = [[ALAssetsLibrary alloc] init];

 void (^assetEnumerator)( ALAsset *, NSUInteger, BOOL *) = ^(ALAsset *result, NSUInteger index, BOOL *stop) {
  if(result != nil) {
   if([[result valueForProperty:ALAssetPropertyType] isEqualToString:ALAssetTypePhoto]) {
    [assetURLDictionaries addObject:[result valueForProperty:ALAssetPropertyURLs]];

    NSURL *url= (NSURL*) [[result defaultRepresentation]url]; 

    [library assetForURL:url
             resultBlock:^(ALAsset *asset) {
              [mutableArray addObject:[UIImage imageWithCGImage:[[asset defaultRepresentation] fullScreenImage]]];

              if ([mutableArray count]==count)
              {
               imageArray=[[NSArray alloc] initWithArray:mutableArray];
               [self allPhotosCollected:imageArray];
              }
             }
            failureBlock:^(NSError *error){ NSLog(@"operation was not successfull!"); } ]; 

   } 
  }
 };

 NSMutableArray *assetGroups = [[NSMutableArray alloc] init];

 void (^ assetGroupEnumerator) ( ALAssetsGroup *, BOOL *)= ^(ALAssetsGroup *group, BOOL *stop) {
  if(group != nil) {
   [group enumerateAssetsUsingBlock:assetEnumerator];
   [assetGroups addObject:group];
   count=[group numberOfAssets];
  }
 };

 assetGroups = [[NSMutableArray alloc] init];

 [library enumerateGroupsWithTypes:ALAssetsGroupAll
                        usingBlock:assetGroupEnumerator
                      failureBlock:^(NSError *error) {NSLog(@"There is an error");}];
}

-(void)allPhotosCollected:(NSArray*)imgArray
{
 //write your code here after getting all the photos from library...
 NSLog(@"all pictures are %@",imgArray);
}

@end
 */
