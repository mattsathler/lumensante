// Appended to the original phone's first frame. Pixel data comes from Assets/logo.png.
var lumenPhonePixels = "__LUMEN_PHONE_PIXELS__";
var lumenPhonePrototype = com.rockstargames.gtav.cellphone.apps.__PHONE_HOME_CLASS__.prototype;
var lumenPhoneOriginalIcon = lumenPhonePrototype.setIcon;
lumenPhonePrototype.setIcon = function(target, icon, alpha)
{
   lumenPhoneOriginalIcon.call(this, target, icon, alpha);
   if(target == undefined || target.homeMenuIcons == undefined)
   {
      return;
   }
   var holder = target.homeMenuIcons;
   if(target.menuIconTitle != "Hospital" || icon != 6)
   {
      if(holder.lumenHospitalLogo != undefined)
      {
         holder.lumenHospitalLogo.removeMovieClip();
      }
      return;
   }
   if(holder.lumenHospitalLogo != undefined)
   {
      return;
   }
   var logo = holder.createEmptyMovieClip("lumenHospitalLogo", holder.getNextHighestDepth());
   logo.beginFill(0x083B2B,100);
   logo.moveTo(-25,-25);
   logo.lineTo(25,-25);
   logo.lineTo(25,25);
   logo.lineTo(-25,25);
   logo.lineTo(-25,-25);
   logo.endFill();
   var x = 0;
   var y = 0;
   var size = 1.5;
   var value;
   var opacity;
   while(y < 32)
   {
      x = 0;
      while(x < 32)
      {
         value = parseInt(lumenPhonePixels.substr((y * 32 + x) * 4,4),16);
         opacity = Math.floor(value / 4096);
         if(opacity > 0)
         {
            var red = Math.floor(value / 256) % 16 * 17;
            var green = Math.floor(value / 16) % 16 * 17;
            var blue = value % 16 * 17;
            logo.beginFill(red * 65536 + green * 256 + blue,opacity * 100 / 15);
            logo.moveTo(-24 + x * size,-24 + y * size);
            logo.lineTo(-24 + (x + 1) * size,-24 + y * size);
            logo.lineTo(-24 + (x + 1) * size,-24 + (y + 1) * size);
            logo.lineTo(-24 + x * size,-24 + (y + 1) * size);
            logo.lineTo(-24 + x * size,-24 + y * size);
            logo.endFill();
         }
         x++;
      }
      y++;
   }
};
