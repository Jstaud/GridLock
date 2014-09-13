//
//  MainViewController.m
//
//  Copyright (c) 2012 Exadel Inc. All rights reserved.
//
#import "MainViewController.h"
#import "ApperyioURLProtocol.h"
#import "MainCommandQueue.h"
#import "CDVReachability.h"

#import <AVFoundation/AVFoundation.h>

@interface MainViewController ()

@property (nonatomic, assign) CGFloat statusBarHeight;
@property (nonatomic, assign) BOOL initializeCompleted;
@property (nonatomic, strong) NSURLRequest *delayedRequest;
@property (nonatomic, strong) CDVReachability* reach;
@property (nonatomic, strong) id rotationObserver;

/**
 * Register ApperyioURLProtocol to configure server trust authentication chalenge
 *     for allow or reject self-signed certificates.
 * This method should be invoked after [CDVViewController viewDidLoad] method
 *     to place ApperyioURLProtocol behind CDVURLProtocol.
 */
- (void)registerCustomURLProtocol;
- (void)unregisterCustomURLProtocol;

- (void)allowBackgroundAudioPlay;
- (BOOL)ignoreRequest:(NSURLRequest*)request;

@end

@implementation MainViewController

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self)
    {
        CGSize sbSize = [[UIApplication sharedApplication] statusBarFrame].size;
        self.statusBarHeight = UIInterfaceOrientationIsPortrait(self.interfaceOrientation) ? sbSize.height : sbSize.width;
        self.initializeCompleted = NO;
        self.delayedRequest = nil;
        
        _commandQueue = [[MainCommandQueue alloc] initWithViewController:self];
        
        self.reach = [CDVReachability reachabilityForInternetConnection];
        //fix for jqm issue #4113
        [[NSNotificationCenter defaultCenter] addObserverForName:UIKeyboardWillShowNotification
                                                          object:nil
                                                           queue:nil
                                                      usingBlock:^(NSNotification *note) {
                                                          [self.webView stringByEvaluatingJavaScriptFromString:@"$(\"div[data-role='footer']\").addClass(\"ui-fixed-hidden\");"];
                                                      }];
    }
    return self;
}

- (void)dealloc
{
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    [self unregisterCustomURLProtocol];
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    [self registerCustomURLProtocol];
    [self allowBackgroundAudioPlay];
    
    self.initializeCompleted = YES;
    self.webView.dataDetectorTypes = UIDataDetectorTypeNone;
    if(nil != self.delayedRequest)
    {
        [self.webView loadRequest:self.delayedRequest];
        self.delayedRequest = nil;
    }
}

- (void) viewDidAppear:(BOOL)animated
{
    [super viewDidAppear:animated];
    
    [[NSNotificationCenter defaultCenter] removeObserver:self.rotationObserver];
    self.rotationObserver = nil;
}

- (void) viewDidDisappear:(BOOL)animated
{
    [super viewDidDisappear:animated];
    //ETST-14838
    if (SYSTEM_VERSION_LESS_THAN(@"7.0"))
    {
        self.rotationObserver = [[NSNotificationCenter defaultCenter] addObserverForName:UIDeviceOrientationDidChangeNotification
                                                                                  object:nil
                                                                                   queue:[NSOperationQueue mainQueue]
                                                                              usingBlock:^(NSNotification *note) {
                                                                                  CGSize screen = [[UIScreen mainScreen] bounds].size;
                                                                                  CGFloat statusBarHeight = [UIApplication sharedApplication].statusBarHidden ? 0 : self.statusBarHeight;
                                                                                  NSLog(@"%f", statusBarHeight);
                                                                                  UIInterfaceOrientation orientation = [[UIApplication sharedApplication] statusBarOrientation];
                                                                                  if (UIDeviceOrientationIsPortrait(orientation))
                                                                                  {
                                                                                      self.view.frame = CGRectMake(0, 0, screen.width, screen.height - statusBarHeight);
                                                                                  }
                                                                                  else
                                                                                  {
                                                                                      self.view.frame = CGRectMake(0, 0, screen.height, screen.width - statusBarHeight);
                                                                                  }
                                                                              }];
    }
}

#pragma mark - Overwrite CDVViewController methods

- (NSArray*)parseInterfaceOrientations:(NSArray*)orientations
{
    if ([orientations count] == 0)
    {
        NSDictionary* infoDict = [[NSBundle mainBundle] infoDictionary];
        NSString* orientationString = [infoDict objectForKey:@"UIInterfaceOrientation"];
        orientations = [NSArray arrayWithObject:orientationString];
    }
    
    return [super parseInterfaceOrientations:orientations];
}

#pragma mark - UIWebViewDelegate

- (void)webView:(UIWebView*)theWebView didFailLoadWithError:(NSError*)error
{
    [super webView:theWebView didFailLoadWithError:error];
    
    //Temporary fix for ETST-16725
    if ([error code] == NSURLErrorCancelled)
    {
        [[NSNotificationCenter defaultCenter] postNotification:[NSNotification notificationWithName:CDVPageDidLoadNotification object:self.webView]];
    }
}

- (BOOL)webView:(UIWebView*)theWebView shouldStartLoadWithRequest:(NSURLRequest*)request navigationType:(UIWebViewNavigationType)navigationType
{
    if([self ignoreRequest:request])
    {
        return NO;
    }
    
    if(self.initializeCompleted)
    {
        return [super webView:theWebView shouldStartLoadWithRequest:request navigationType:navigationType];
    }
    
    self.delayedRequest = request;
    return NO;
}

#pragma mark - Utility methods

- (void)registerCustomURLProtocol
{
    BOOL allowAllCertificates = [(NSNumber *)[self.settings objectForKey:@"allowallhttpscertificates"] boolValue];
    [ApperyioURLProtocol registerWithWhiteList:self.whitelist allowAllCertificates:allowAllCertificates];
}

- (void)unregisterCustomURLProtocol
{
    [ApperyioURLProtocol unregisterClass:[ApperyioURLProtocol class]];
}

- (void)allowBackgroundAudioPlay
{
    NSError *setBackgroundAudioPlayError = nil;
    
    [[AVAudioSession sharedInstance] setCategory:AVAudioSessionCategoryPlayback error:&setBackgroundAudioPlayError];
    if (setBackgroundAudioPlayError != nil)
    {
        NSLog(@"Cannot activate audio background playing due to error: %@.", setBackgroundAudioPlayError);
        return;
    }
}

- (BOOL)ignoreRequest:(NSURLRequest*)request
{
    NSString *scheme = request.URL.scheme;
    return ([scheme isEqualToString:@"http"] ||
            [scheme isEqualToString:@"https"]||
            [scheme isEqualToString:@"ftp"]  ||
            [scheme isEqualToString:@"ftps"])&&
    ![self.reach currentReachabilityStatus];
}

- (UIWebView*)newCordovaViewWithFrame:(CGRect)bounds
{
    if ([self respondsToSelector:NSSelectorFromString(@"setNeedsStatusBarAppearanceUpdate")])
    {
        if (self.statusBarHeight > 0.)
        {
            bounds = CGRectMake(0, self.statusBarHeight , bounds.size.width, bounds.size.height - self.statusBarHeight);
        }
    }
    return [[UIWebView alloc] initWithFrame:bounds];
}

@end
