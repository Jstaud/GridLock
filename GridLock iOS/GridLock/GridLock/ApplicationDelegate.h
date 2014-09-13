//
//  ApplicationDelegate.h
//
//  Created by Pavel Gorb on 9/4/11.
//  Copyright 2011 Exadel Inc. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <Cordova/CDVViewController.h>

@interface ApplicationDelegate : NSObject < UIApplicationDelegate, UIWebViewDelegate>

// invoke string is passed to your app on launch, this is only valid if you 
// edit Info.plist to add a protocol
// a simple tutorial can be found here : 
// http://iphonedevelopertips.com/cocoa/launching-your-own-application-via-a-custom-url-scheme.html

@property (copy)  NSString* invokeString;
@property (nonatomic, strong) IBOutlet UIWindow* window;
@property (nonatomic, strong) IBOutlet CDVViewController* viewController;

@end
