// First frame of the separate website movie requested by the native browser.
var TIMELINE = {};
TIMELINE.IS_SITE_DYNAMIC = 0;
TIMELINE.PAGE_NAMES = [null, "PAGE1"];
TIMELINE.CAN_STORE_PAGE = [false, true];
TIMELINE.resetScroll = true;
TIMELINE.CONTENT = this.createEmptyMovieClip("CONTENT", this.getNextHighestDepth());
var bounds = TIMELINE.CONTENT.createEmptyMovieClip("BOUNDING_BOX", 0);
var hospitalPage = TIMELINE.CONTENT.createEmptyMovieClip("hospitalPage", 1);

function hospitalRectangle(clip, x, y, width, height, color)
{
   clip.beginFill(color);
   clip.moveTo(x, y);
   clip.lineTo(x + width, y);
   clip.lineTo(x + width, y + height);
   clip.lineTo(x, y + height);
   clip.lineTo(x, y);
   clip.endFill();
}

function hospitalLabel(name, value, x, y, width, height, size, color, bold)
{
   hospitalPage.createTextField(name, hospitalPage.getNextHighestDepth(), x, y, width, height);
   var field = hospitalPage[name];
   field.selectable = false;
   field.multiline = true;
   field.wordWrap = true;
   field.text = value;
   field.setTextFormat(new TextFormat("$Font2", size, color, bold));
}

hospitalRectangle(bounds, 0, 0, 1280, 627, 0xF4F0E9);
hospitalRectangle(hospitalPage, 0, 0, 1280, 627, 0xF4F0E9);
hospitalRectangle(hospitalPage, 0, 0, 1280, 237, 0x103A30);
hospitalRectangle(hospitalPage, 213, 70, 40, 4, 0xD6B673);
hospitalLabel("brand", "Los Santos Medical Center", 208, 89, 860, 74, 45, 0xFFFFFF, true);
hospitalLabel("hospital", "Central Los Santos Medical Center", 211, 169, 860, 44, 24, 0xE8DFCE, false);
hospitalRectangle(hospitalPage, 212, 284, 856, 144, 0xFFFFFF);
hospitalLabel("heading", "Central de atendimento", 239, 309, 800, 46, 29, 0x103A30, true);
hospitalLabel("description", "O acesso a prontuários, laudos e agenda será integrado ao plantão.", 239, 368, 800, 40, 19, 0x4A5E56, false);

TIMELINE.displayPage = function(pageName)
{
   this.resetScroll = true;
   _level0.TIMELINE.SET_PAGE_BUTTONS([]);
   _level0.TIMELINE.SET_TITLEBAR_TEXT("Los Santos Medical Center");
};
TIMELINE.POPULATE_TEXT = function(pageName)
{
   this.displayPage(pageName);
};
TIMELINE.CLEANUP = function() {};
