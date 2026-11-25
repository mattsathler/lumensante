if (!TIMELINE.isMP)
{
   TIMELINE.lumenAgendaView = 0;
   TIMELINE.lumenAgendaOffset = 0;
   TIMELINE.lumenAgendaPage = 0;
   TIMELINE.lumenAgendaConfirm = "";
   TIMELINE.lumenAgendaOwner = "";

   TIMELINE.lumenRenderAgenda = function(newPage)
   {
      if (newPage)
      {
         this.lumenAgendaView = 0;
         this.lumenAgendaOffset = 0;
         this.lumenAgendaPage = 0;
         this.lumenAgendaConfirm = "";
      }
      var content = this.CONTENT;
      var data = this.dataProviderUI[95];
      if (data == undefined || data.length < 7 || String(data[1]).length != 32)
      {
         if (content.lumenAgenda == undefined) this.lumenShowLoading("Los Santos Medical Center · Agenda");
         return;
      }
      content.lumenLoading.removeMovieClip();
      if (data != undefined && this.lumenAgendaOwner != data[1])
      {
         this.lumenAgendaOwner = data[1];
         this.lumenAgendaView = 0;
         this.lumenAgendaOffset = 0;
         this.lumenAgendaPage = 0;
         this.lumenAgendaConfirm = "";
      }
      var signature = this.lumenAgendaView + "|" + this.lumenAgendaOffset + "|" +
         this.lumenAgendaPage + "|" + this.lumenAgendaConfirm;
      if (data != undefined)
      {
         signature += "|" + data.slice(0, 5).join(",") + "|" + data[6];
         for (var i = 0; i < data[4]; i++)
            signature += "|" + String(this.dataProviderUI[5000 + i]);
      }
      if (!newPage && content.lumenAgenda != undefined && signature == this.lumenAgendaSignature)
         return;
      this.lumenAgendaSignature = signature;
      content.lumenAgenda.removeMovieClip();
      var view = content.createEmptyMovieClip("lumenAgenda", content.getNextHighestDepth());
      view._x = 96;
      view._y = -36;
      view._xscale = 85;
      var site = this;
      var browser = this.browser;
      var buttons = [];

      function rect(x, y, width, height, color)
      {
         var shape = view.createEmptyMovieClip("shape" + view.getNextHighestDepth(), view.getNextHighestDepth());
         shape._x = x;
         shape._y = y;
         shape.beginFill(color);
         shape.moveTo(0, 0);
         shape.lineTo(width, 0);
         shape.lineTo(width, height);
         shape.lineTo(0, height);
         shape.lineTo(0, 0);
         shape.endFill();
         return shape;
      }
      function label(name, value, x, y, width, height, size, color, bold)
      {
         view.createTextField(name, view.getNextHighestDepth(), x, y, width, height);
         var field = view[name];
         field.selectable = false;
         field.text = value;
         field.setTextFormat(new TextFormat("$Font2", size, color, bold));
      }
      function button(name, text, x, y, action, value)
      {
         var control = view.attachMovie("button_WOMtextLink", name, view.getNextHighestDepth(), {_x:x, _y:y});
         control.btnTxt.text = text;
         control.btnTxt.autoSize = "left";
         control.offColour = 0x17624D;
         control.onColour = 0xB48E45;
         control.lumenAction = action;
         control.lumenValue = value;
         control.buttonEvent = function()
         {
            if (this.lumenAction == "home")
               browser.GO_TO_WEBPAGE("WWW_LUMENSANTE_COM");
            else if (this.lumenAction == "patient")
            {
               site.lumenAgendaPatientId = this.lumenValue;
               browser.GO_TO_WEBPAGE("WWW_LUMENSANTE_COM_S_PACIENTES");
            }
            else if (this.lumenAction == "cancel")
            {
               if (site.lumenAgendaConfirm != this.lumenValue)
                  site.lumenAgendaConfirm = this.lumenValue;
               else
               {
                  var current = site.dataProviderUI[95];
                  for (var itemIndex = 0; current != undefined && itemIndex < current[4] && itemIndex < 4095; itemIndex++)
                  {
                     var item = site.dataProviderUI[5000 + itemIndex];
                     if (item != undefined && item[0] == this.lumenValue && item[5] == 5)
                     {
                        browser.setFlags(17, current[5] * 4096 + itemIndex + 1);
                        break;
                     }
                  }
                  site.lumenAgendaConfirm = "";
               }
               site.lumenRenderAgenda(false);
            }
            else
            {
               if (this.lumenAction == "view")
               {
                  site.lumenAgendaView = this.lumenValue;
                  site.lumenAgendaOffset = 0;
                  site.lumenAgendaPage = 0;
               }
               else if (this.lumenAction == "offset")
               {
                  site.lumenAgendaOffset = Math.max(-52, Math.min(52, this.lumenValue));
                  site.lumenAgendaPage = 0;
               }
               else if (this.lumenAction == "page")
                  site.lumenAgendaPage = this.lumenValue;
               site.lumenAgendaConfirm = "";
               site.CONTENT._y = 0;
               site.lumenRenderAgenda(false);
            }
         };
         buttons.push(control.btnTxt);
         return control;
      }
      function monthLength(year, month)
      {
         if (month == 2)
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28;
         return month == 4 || month == 6 || month == 9 || month == 11 ? 30 : 31;
      }
      function formatDateOffset(value, offset)
      {
         var parts = String(value).split("/");
         if (parts.length != 3) return "Data indisponível";
         var day = Number(parts[0]);
         var month = Number(parts[1]);
         var year = Number(parts[2]);
         if (isNaN(day) || isNaN(month) || isNaN(year) || year < 1 ||
             month < 1 || month > 12 || day < 1 || day > monthLength(year, month))
            return "Data indisponível";
         day += offset;
         while (day > monthLength(year, month))
         {
            day -= monthLength(year, month);
            month++;
            if (month > 12) { month = 1; year++; }
         }
         while (day < 1)
         {
            month--;
            if (month < 1) { month = 12; year--; }
            day += monthLength(year, month);
         }
         return (day < 10 ? "0" : "") + day + "/" + (month < 10 ? "0" : "") + month + "/" + year;
      }
      function status(value)
      {
         var labels = ["Agendado", "Aguardando entrada", "Na recepção", "Em atendimento",
            "Atendido", "Pendente", "Cancelado", "Consulta perdida"];
         return labels[value] == undefined ? "Agendado" : labels[value];
      }

      var fullX = -96 / 0.85;
      var fullWidth = 1280 / 0.85;
      var background = rect(fullX, 0, fullWidth, 740, 0xEDF0ED);
      var bottom = 740;
      button("home", "Início  ›", 176, 143, "home", 0);
      label("heading", "Agenda", 176, 174, 800, 52, 30, 0x103A30, true);

      var tabs = ["Dia", "Semana", "Retornos pendentes"];
      for (i = 0; i < tabs.length; i++)
      {
         var tabX = 176 + i * 225;
         rect(tabX, 231, 212, 43, i == this.lumenAgendaView ? 0xDCEAE2 : 0xFFFFFF);
         this.lumenDrawIcon(view,"agendaTabIcon" + i,i == 2 ? "history" : "calendar",tabX + 12,243,18,0x17624D);
         button("tab" + i, "     " + tabs[i], tabX + 12, 242, "view", i);
      }
      if (data == undefined || data[0] != 1)
         label("unavailable", "Agenda indisponível.", 195, 306, 850, 36, 19, 0x526E61, false);
      else
      {
         var from = this.lumenAgendaView == 1 ? this.lumenAgendaOffset * 7 - data[3] : this.lumenAgendaOffset;
         var until = from + (this.lumenAgendaView == 1 ? 7 : 1);
         var heading = this.lumenAgendaView == 2 ? "Retornos pendentes" :
            this.lumenAgendaView == 1 ? formatDateOffset(data[2], from) + " a " + formatDateOffset(data[2], until - 1) : formatDateOffset(data[2], from);
         label("date", heading, 286, 295, 700, 30, 19, 0x103A30, true);
         if (this.lumenAgendaView != 2)
         {
            var previousDate = button("previousDate", "   ", 195, 291, "offset", this.lumenAgendaOffset - 1);
            var nextDate = button("nextDate", "   ", 1050, 291, "offset", this.lumenAgendaOffset + 1);
            previousDate.btnTxt.autoSize = false; previousDate.btnTxt._width = 36; previousDate.btnTxt._height = 32;
            nextDate.btnTxt.autoSize = false; nextDate.btnTxt._width = 36; nextDate.btnTxt._height = 32;
            this.lumenDrawIcon(view,"previousDateIcon","left",200,295,24,0x17624D);
            this.lumenDrawIcon(view,"nextDateIcon","right",1055,295,24,0x17624D);
         }
         var entries = [];
         for (i = 0; i < data[4]; i++)
         {
            var row = this.dataProviderUI[5000 + i];
            if (row != undefined && (this.lumenAgendaView == 2 ? row[5] == 5 : row[3] >= from && row[3] < until))
               entries.push(row);
         }
         if (entries.length == 0) label("empty", "Nenhuma consulta nesta lista.", 195, 347, 800, 30, 18, 0x526E61, false);
         for (i = 0; i < entries.length; i++)
         {
            row = entries[i];
            var y = 335 + i * 51;
            rect(176, y, 928, 46, 0xFFFFFF);
            label("name" + i, row[2], 193, y + 3, 410, 22, 16, 0x103A30, true);
            label("detail" + i, row[4] + " · " + (row[6] == 1 ? "Retorno" : "Consulta") + " · " + status(row[5]),
               193, y + 25, 660, 20, 13, 0x526E61, false);
            if (this.lumenAgendaView == 2)
            {
               if (row[8] == 1)
                  button("patient" + i, "Prontuário  ›", 690, y + 12, "patient", row[1]);
               button("cancel" + i, this.lumenAgendaConfirm == row[0] ? "Confirmar" : "Excluir retorno", 870, y + 12, "cancel", row[0]);
            }
            else if (row[8] == 1)
               button("patient" + i, "Prontuário  ›", 905, y + 12, "patient", row[1]);
         }
         var feedbackY = Math.max(350, 335 + entries.length * 51 + 15);
         if (data[6] != "") label("feedback", data[6], 176, feedbackY, 850, 24, 14, 0x17624D, false);
         bottom = Math.max(bottom, feedbackY + 50);
      }
      background._height = bottom;
      content.BOUNDING_BOX._height = bottom - 36;
      browser.RESIZE_WEBSITE();
      content._y = Math.max(627 - bottom + 36, Math.min(0, content._y));
      this.lumenSetPageButtons("AGENDA", buttons);
      view.lumenButtons = buttons;
      view.lumenCheckFrames = 180;
      view.onEnterFrame = function()
      {
         this.lumenCheckFrames--;
         if (this.lumenCheckFrames % 10 == 0)
         {
            var header = site.CONTENT.lumenHeader;
            var registered = browser.buttonManager.buttonMCList;
            var headerCount = header == undefined ? 0 : header.lumenButtons.length;
            if (header == undefined || registered == undefined || registered.length != headerCount + this.lumenButtons.length ||
                (headerCount > 0 && registered[0] != header.lumenButtons[0]._parent) ||
                (this.lumenButtons.length > 0 && registered[headerCount] != this.lumenButtons[0]._parent))
               site.lumenSetPageButtons("AGENDA", this.lumenButtons);
         }
         if (this.lumenCheckFrames <= 0) delete this.onEnterFrame;
      };
      browser.SET_TITLEBAR_TEXT("Los Santos Medical Center · Agenda");
   };
}
