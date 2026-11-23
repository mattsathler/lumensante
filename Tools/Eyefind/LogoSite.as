// Pixel data is generated from Assets/logo.png by Build-NativeEyefind.ps1.
if (!TIMELINE.isMP)
{
   TIMELINE.lumenLogoPixels = "__LUMEN_LOGO_PIXELS__";
   TIMELINE.lumenIconMasks = {__LUMEN_ICON_MASKS__};
   TIMELINE.lumenDrawIcon = function(parent, name, asset, x, y, size, color)
   {
      var clip = parent.createEmptyMovieClip(name, parent.getNextHighestDepth());
      clip._x = x; clip._y = y;
      var mask = this.lumenIconMasks[asset];
      if (mask == undefined) return clip;
      var cell = size / 24;
      for (var row = 0; row < 24; row++)
      {
         var column = 0;
         while (column < 24)
         {
            var pixel = mask.charAt(row * 24 + column);
            var end = column + 1;
            while (end < 24 && mask.charAt(row * 24 + end) == pixel) end++;
            var alpha = parseInt(pixel, 16);
            if (alpha > 0)
            {
               clip.beginFill(color, alpha * 100 / 15);
               clip.moveTo(column * cell, row * cell);
               clip.lineTo(end * cell, row * cell);
               clip.lineTo(end * cell, (row + 1) * cell);
               clip.lineTo(column * cell, (row + 1) * cell);
               clip.lineTo(column * cell, row * cell);
               clip.endFill();
            }
            column = end;
         }
      }
      return clip;
   };
   TIMELINE.lumenDrawLogo = function(parent, name, x, y, size)
   {
      var clip = parent.createEmptyMovieClip(name, parent.getNextHighestDepth());
      clip._x = x;
      clip._y = y;
      var pixels = this.lumenLogoPixels;
      var cell = size / 64;
      for (var row = 0; row < 64; row++)
      {
         var column = 0;
         while (column < 64)
         {
            var offset = (row * 64 + column) * 4;
            var pixel = pixels.substr(offset, 4);
            var end = column + 1;
            while (end < 64 && pixels.substr((row * 64 + end) * 4, 4) == pixel) end++;
            var alpha = parseInt(pixel.substr(0, 1), 16);
            if (alpha > 0)
            {
               var red = parseInt(pixel.substr(1, 1), 16) * 17;
               var green = parseInt(pixel.substr(2, 1), 16) * 17;
               var blue = parseInt(pixel.substr(3, 1), 16) * 17;
               clip.beginFill(red * 65536 + green * 256 + blue, alpha * 100 / 15);
               clip.moveTo(column * cell, row * cell);
               clip.lineTo(end * cell, row * cell);
               clip.lineTo(end * cell, (row + 1) * cell);
               clip.lineTo(column * cell, (row + 1) * cell);
               clip.lineTo(column * cell, row * cell);
               clip.endFill();
            }
            column = end;
         }
      }
      return clip;
   };
}
