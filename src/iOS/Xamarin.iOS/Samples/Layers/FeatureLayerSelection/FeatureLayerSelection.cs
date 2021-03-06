// Copyright 2016 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;
using System.Drawing;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using UIKit;

namespace ArcGISRuntime.Samples.FeatureLayerSelection
{
    [Register("FeatureLayerSelection")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Feature layer selection",
        "Layers",
        "Select features by tapping a MapView.",
        "")]
    public class FeatureLayerSelection : UIViewController
    {
        // Create and hold references to the UI controls.
        private MapView _myMapView;

        // Hold reference to the feature layer.
        private FeatureLayer _featureLayer;

        public FeatureLayerSelection()
        {
            Title = "Feature layer Selection";
        }

        public override void LoadView()
        {
            _myMapView = new MapView();
            _myMapView.TranslatesAutoresizingMaskIntoConstraints = false;

            View = new UIView();
            View.AddSubviews(_myMapView);

            _myMapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            _myMapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
            _myMapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _myMapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialize();
        }

        private async void Initialize()
        {
            // Create new Map with basemap.
            Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            // Create envelope to be used as a target extent for map's initial viewpoint.
            Envelope myEnvelope = new Envelope(-6603299.491810, 1679677.742046, 9002253.947487, 8691318.054732, SpatialReferences.WebMercator);

            // Set the initial viewpoint for map.
            myMap.InitialViewpoint = new Viewpoint(myEnvelope);

            // Provide used Map to the MapView.
            _myMapView.Map = myMap;

            // Set the selection color.
            _myMapView.SelectionProperties.Color = Color.Cyan;

            // Create Uri for the feature service.
            Uri featureServiceUri = new Uri(
                "https://services1.arcgis.com/4yjifSiIG17X0gW4/arcgis/rest/services/GDP_per_capita_1960_2016/FeatureServer/0");

            // Initialize feature table using a URL to feature server.
            ServiceFeatureTable featureTable = new ServiceFeatureTable(featureServiceUri);

            // Initialize a new feature layer based on the feature table.
            _featureLayer = new FeatureLayer(featureTable);

            try
            {
                // Make sure that used feature layer is loaded before hooking into the tapped event
                // This prevents trying to do selection on the layer that isn't initialized.
                await _featureLayer.LoadAsync();

                // Check for the load status. If the layer is loaded then add it to map.
                if (_featureLayer.LoadStatus == LoadStatus.Loaded)
                {
                    // Add the feature layer to the map.
                    myMap.OperationalLayers.Add(_featureLayer);

                    // Add tap event handler for mapview.
                    _myMapView.GeoViewTapped += OnMapViewTapped;
                }
            }
            catch (Exception e)
            {
                new UIAlertView("Error", e.ToString(), (IUIAlertViewDelegate) null, "OK", null).Show();
            }
        }

        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                // Define the selection tolerance.
                double tolerance = 15;

                // Convert the tolerance to map units.
                double mapTolerance = tolerance * _myMapView.UnitsPerPixel;

                // Get the tapped point.
                MapPoint geometry = e.Location;

                // Normalize the geometry if wrap-around is enabled.
                //    This is necessary because of how wrapped-around map coordinates are handled by Runtime.
                //    Without this step, querying may fail because wrapped-around coordinates are out of bounds.
                if (_myMapView.IsWrapAroundEnabled)
                {
                    geometry = (MapPoint) GeometryEngine.NormalizeCentralMeridian(geometry);
                }

                // Define the envelope around the tap location for selecting features.
                Envelope selectionEnvelope = new Envelope(geometry.X - mapTolerance, geometry.Y - mapTolerance, geometry.X + mapTolerance,
                    geometry.Y + mapTolerance, _myMapView.Map.SpatialReference);

                // Define the query parameters for selecting features.
                QueryParameters queryParams = new QueryParameters
                {
                    // Set the geometry to selection envelope for selection by geometry.
                    Geometry = selectionEnvelope
                };

                // Select the features based on query parameters defined above.
                await _featureLayer.SelectFeaturesAsync(queryParams, SelectionMode.New);
            }
            catch (Exception ex)
            {
                new UIAlertView("Error", ex.ToString(), (IUIAlertViewDelegate) null, "OK", null).Show();
            }
        }
    }
}