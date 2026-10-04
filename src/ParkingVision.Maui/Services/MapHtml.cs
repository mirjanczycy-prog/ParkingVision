namespace ParkingVision.Maui.Services;

/// <summary>
/// The map page: Leaflet + OpenStreetMap tiles inside a WebView. C# talks to it in two directions:
///   C# -> JS : WebView.EvaluateJavaScriptAsync("pvSetZones(zones, lat, lon, radius, labels)")
///   JS -> C# : a link to pvapp://zone?id=N, caught in WebView.Navigating (and cancelled).
/// Leaflet is loaded from a CDN, so the map needs internet (the list does not). See docs/07, "Mapa (OpenStreetMap)".
/// </summary>
public static class MapHtml
{
    public static string Build(string tileUrl, string offlineText) =>
        Template.Replace("__TILE__", JsEscape(tileUrl)).Replace("__OFFLINE__", HtmlEscape(offlineText));

    private static string JsEscape(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ");
    private static string HtmlEscape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private const string Template = """
<!doctype html>
<html><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no">
<meta name="referrer" content="origin">
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
<style>
html,body,#map{height:100%;margin:0;background:#E7ECEF;font-family:-apple-system,Roboto,Arial,sans-serif}
.pvpop b{display:block;font-size:15px;color:#1E2A36;margin-bottom:2px}
.pvpop span{font-size:13px;color:#5B6773}
.pvbtn{display:inline-block;margin-top:8px;padding:8px 12px;border-radius:8px;background:#1E2A36;color:#F4C430 !important;font-weight:700;text-decoration:none;font-size:13px}
.pvoff{padding:24px;font-size:15px;color:#5B6773}
</style></head>
<body><div id="map"></div>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script>
(function(){
if(typeof L==='undefined'){document.body.innerHTML='<p class="pvoff">__OFFLINE__</p>';return;}
var map=L.map('map',{zoomControl:false}).setView([50.06,19.94],13);
L.tileLayer('__TILE__',{maxZoom:19,attribution:'&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'}).addTo(map);
var layer=L.layerGroup().addTo(map), me=null, lastKey=null;
function esc(s){return String(s).replace(/[&<>]/g,function(c){return {'&':'&amp;','<':'&lt;','>':'&gt;'}[c];});}
window.pvSetZones=function(zones,lat,lon,radius,labels){
  layer.clearLayers();
  var pts=[];
  zones.forEach(function(z){
    var html='<div class="pvpop"><b>'+esc(z.name)+'</b><span>'+esc(z.free)+' / '+esc(z.total)+' &middot; '+esc(z.badge)+'</span><br><a class="pvbtn" href="pvapp://zone?id='+z.id+'">'+esc(labels.details)+'</a></div>';
    L.circle([z.lat,z.lon],{radius:70,color:z.color,fillColor:z.color,fillOpacity:0.4,weight:4}).addTo(layer).bindPopup(html);
    L.circleMarker([z.lat,z.lon],{radius:9,color:'#ffffff',weight:3,fillColor:z.color,fillOpacity:1}).addTo(layer).bindPopup(html);
    pts.push([z.lat,z.lon]);
  });
  if(me){map.removeLayer(me);}
  me=L.circleMarker([lat,lon],{radius:7,color:'#ffffff',weight:3,fillColor:'#1E2A36',fillOpacity:1}).addTo(map);
  var key=lat.toFixed(5)+','+lon.toFixed(5)+','+radius+','+zones.length;
  if(key!==lastKey){
    lastKey=key;
    if(pts.length){pts.push([lat,lon]);map.fitBounds(L.latLngBounds(pts),{padding:[56,56],maxZoom:17});}
    else{map.setView([lat,lon],14);}
  }
};
})();
</script></body></html>
""";
}
