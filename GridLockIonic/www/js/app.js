// Ionic Starter App

// angular.module is a global place for creating, registering and retrieving Angular modules
// 'starter' is the name of this angular module example (also set in a <body> attribute in index.html)
// the 2nd parameter is an array of 'requires'
angular.module('starter', ['ionic'])



.run(function ($ionicPlatform) {
  $ionicPlatform.ready(function () {
    // Hide the accessory bar by default (remove this to show the accessory bar above the keyboard
    // for form inputs)
    if (window.cordova && window.cordova.plugins.Keyboard) {
      cordova.plugins.Keyboard.hideKeyboardAccessoryBar(true);
    }
    if (window.StatusBar) {
      StatusBar.styleDefault();
    }
  });
})


.controller('locationCtrl', function($scope, $ionicLoading, $interval) {

  function initialize() {

    $scope.data = [
      { 
        goalSpeed: 55,
        speedLimit: 60,
        currentSpeed: 0,
        units: 'MPH',
        lat: 0,
        lng: 0,
        direction: ''
      }
    ];

    // var mapOptions = {
    //   center: myLatlng,
    //   zoom: 16,
    //   mapTypeId: google.maps.MapTypeId.ROADMAP
    // };

    // var map = new google.maps.Map(mapOptions);

    // $scope.map = map;
  }
  
  $scope.getCurrentPosition = function(gotPosition, gotSpeed, gotDirection) {

    navigator.geolocation.getCurrentPosition(function(pos) {
      if ($scope.loading) {
        $scope.loading.hide();
      };

      gotPosition(pos.coords.latitude, pos.coords.longitude);
      gotSpeed(pos.coords.speed);
      gotDirection(pos.coords.heading);
    }, 
    function(error) {
      $scope.loading = $ionicLoading.show({
        template: 'Cannot get current location. \nRetrying...',
        showBackdrop: true
      });
    });
  };

  
  window.addEventListener('load', initialize);
  
  $interval(function() {
    $scope.getCurrentPosition(function(lat, lng) {
      console.log(lat + ", " + lng + ", " );
      $scope.data[0]['lat'] = lat;
      $scope.data[0]['lng'] = lng;
    },
    function(speed) {
      console.log("speed: ", speed);
      $scope.data[0]['speedLimit'] = speed ? speed : "no speed";
    },
    function(direction) {
      console.log("direction: ", direction);
    });

  }, 2000);

});



