using System.Collections.Generic;
using UnityEngine;

public enum Language { English, Uzbek }

// O'yin matnlari ikki tilda. Standart til — ingliz; sozlamalardan o'zbekchani tanlash mumkin.
// Yangi matn qo'shish: jadvalga kalit va ikki tarjimani yozing, kodda Loc.T("kalit") yoki Loc.F("kalit", ...) ishlating.
public static class Loc
{
    const string PrefKey = "language";

    // Til almashganda chaqiriladi (3D yozuvlar shunda o'zini yangilaydi)
    public static event System.Action Changed;

    static bool loaded;
    static Language current;

    public static Language Current
    {
        get
        {
            if (!loaded)
            {
                current = (Language)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)Language.English), 0, 1);
                loaded = true;
            }
            return current;
        }
        set
        {
            if (loaded && current == value) return;
            current = value;
            loaded = true;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }

    public static string T(string key)
    {
        string[] texts;
        if (!Table.TryGetValue(key, out texts))
        {
            Debug.LogWarning("Tarjima topilmadi: " + key);
            return key;
        }
        return texts[(int)Current];
    }

    public static string F(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    // { kalit, { inglizcha, o'zbekcha } }
    static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // Ovqatlar
        { "item.Bun", new[] { "Burger bun", "Burger noni" } },
        { "item.Patty", new[] { "Patty", "Kotlet" } },
        { "item.Cheese", new[] { "Cheese", "Pishloq" } },
        { "item.Tomato", new[] { "Tomato", "Pomidor" } },
        { "item.LavashBread", new[] { "Lavash bread", "Lavash noni" } },
        { "item.Meat", new[] { "Meat", "Go'sht" } },
        { "item.Sauce", new[] { "Sauce", "Sous" } },
        { "item.Burger", new[] { "Burger", "Burger" } },
        { "item.Lavash", new[] { "Lavash", "Lavash" } },
        { "item.pattyRaw", new[] { "Raw patty", "Xom kotlet" } },
        { "item.pattyCooked", new[] { "Cooked patty", "Pishgan kotlet" } },
        { "item.pattyBurnt", new[] { "Burnt patty", "Kuygan kotlet" } },
        { "item.meatRaw", new[] { "Raw meat", "Xom go'sht" } },
        { "item.meatCooked", new[] { "Grilled meat", "Qovurilgan go'sht" } },
        { "item.meatBurnt", new[] { "Burnt meat", "Kuygan go'sht" } },
        { "item.lavashRaw", new[] { "Unpressed lavash", "Presslanmagan lavash" } },
        { "item.lavashCooked", new[] { "Ready lavash", "Tayyor lavash" } },
        { "item.lavashBurnt", new[] { "Burnt lavash", "Kuygan lavash" } },
        { "item.pattySpoiled", new[] { "Spoiled patty", "Aynigan kotlet" } },
        { "item.meatSpoiled", new[] { "Spoiled meat", "Aynigan go'sht" } },
        { "spoiled.reject", new[] { "It has gone bad — throw it in the trash!", "Bu aynib qolgan — axlatga tashlang!" } },
        { "item.Coffee", new[] { "Coffee", "Qahva" } },
        { "item.PaperBag", new[] { "Paper bags", "Qog'oz paket" } },
        { "item.CoffeeBeans", new[] { "Coffee beans", "Qahva doni" } },
        { "item.Extinguisher", new[] { "Fire extinguisher", "O't o'chirgich" } },
        { "item.DirtyPlate", new[] { "Dirty plate", "Iflos tarelka" } },
        { "item.onPlate", new[] { "(on a plate)", "(tarelkada)" } },
        { "item.inBag", new[] { "(in a bag)", "(paketda)" } },

        // Yaxshilanishlar
        { "upgrade.0", new[] { "Grill +2 slots", "Gril +2 joy" } },
        { "upgrade.0.desc", new[] { "Cook 4 things on the grill at once", "Grilda bir vaqtda 4 ta pishiring" } },
        { "upgrade.1", new[] { "Strong fire", "Kuchli olov" } },
        { "upgrade.1.desc", new[] { "Grill and press work 1.5x faster", "Gril va press 1.5 baravar tez ishlaydi" } },
        { "upgrade.2", new[] { "Second press slot", "Ikkinchi press" } },
        { "upgrade.2.desc", new[] { "2 slots in the lavash press", "Lavash pressida 2 ta joy" } },
        { "upgrade.3", new[] { "Third board", "Uchinchi taxta" } },
        { "upgrade.3.desc", new[] { "One more assembly board", "Yana bitta yig'ish taxtasi" } },
        { "upgrade.4", new[] { "Advertising", "Reklama" } },
        { "upgrade.4.desc", new[] { "Customers come more often", "Mijozlar tez-tez keladi" } },
        { "upgrade.5", new[] { "Cozy decor", "Shinam dekor" } },
        { "upgrade.5.desc", new[] { "Customers wait 30% longer", "Mijozlar 30% ko'proq kutadi" } },

        // Harakatlar (E tugmasi)
        { "act.take", new[] { "Take: {0}", "Olish: {0}" } },
        { "act.putDown", new[] { "Put down: {0}", "Qo'yish: {0}" } },
        { "act.place", new[] { "Place: {0}", "Qo'yish: {0}" } },
        { "act.putBack", new[] { "Put back", "Qaytarib qo'yish" } },
        { "act.trash", new[] { "Throw away: {0}", "Axlatga tashlash: {0}" } },
        { "act.serve", new[] { "Serve: {0}", "Berish: {0}" } },
        { "act.add", new[] { "Add: {0}", "Qo'shish: {0}" } },
        { "act.closeBurger", new[] { "Close the burger", "Burgerni yopish" } },
        { "act.rollLavash", new[] { "Roll the lavash", "Lavashni o'rash" } },
        { "act.takeBack", new[] { "Take back: {0}", "Olib qo'yish: {0}" } },
        { "act.openShop", new[] { "Open the shop", "Do'konni ochish" } },
        { "act.breakShop", new[] { "Close the shop for a break", "Do'konni tanaffusga yopish" } },
        { "act.endDay", new[] { "Close the shop and end the day", "Do'konni yopib, kunni yakunlash" } },
        { "act.computer", new[] { "Computer: orders and upgrades", "Kompyuter: buyurtma va yaxshilanishlar" } },

        // Stansiyalar
        { "station.grill", new[] { "Grill", "Gril" } },
        { "station.press", new[] { "Press", "Press" } },
        { "station.onlyAccepts", new[] { "The {0} only takes: {1}", "{0}ga faqat: {1}" } },
        { "station.full", new[] { "All slots are taken", "Hamma joy band" } },
        { "station.fullToast", new[] { "The {0} is full", "{0}da hamma joy band" } },
        { "bin.count", new[] { "{0} pcs", "{0} ta" } },
        { "bin.inStock", new[] { "In stock: {0}", "Zaxirada: {0} ta" } },
        { "bin.empty", new[] { "{0} is out — order more on the computer", "{0} tugagan — kompyuterdan buyurtma bering" } },
        { "bin.ranOut", new[] { "{0} ran out! Order more on the computer.", "{0} tugadi! Kompyuterdan buyurtma bering." } },
        { "trash.info", new[] { "Trash bin", "Axlat qutisi" } },
        { "sign.prep", new[] { "START", "OCHISH" } },
        { "sign.open", new[] { "OPEN", "OCHIQ" } },
        { "sign.closed", new[] { "CLOSED", "YOPIQ" } },
        { "sign.infoPrep", new[] { "When you're ready, open up and customers will start coming", "Tayyor bo'lsangiz — oching, mijozlar kela boshlaydi" } },
        { "sign.infoOpen", new[] { "The shop is open, time {0}", "Do'kon ochiq, soat {0}" } },
        { "sign.infoBreak", new[] { "Closed for a break, time {0} — no new customers", "Tanaffus, soat {0} — yangi mijoz kelmaydi" } },
        { "sign.infoAfter", new[] { "Working hours are over — close the shop to end the day", "Ish vaqti tugadi — kunni yakunlash uchun do'konni yoping" } },
        { "sign.infoAfterBusy", new[] { "Working hours are over — serve the remaining customers first", "Ish vaqti tugadi — avval qolgan mijozlarga xizmat qiling" } },
        { "label.grill", new[] { "GRILL", "GRIL" } },
        { "label.press", new[] { "PRESS", "PRESS" } },
        { "label.trash", new[] { "TRASH", "AXLAT" } },
        { "label.orders", new[] { "ORDERS", "BUYURTMA" } },
        { "label.fridge", new[] { "FRIDGE", "MUZLATGICH" } },
        { "label.sink", new[] { "SINK", "RAKOVINA" } },
        { "label.coffee", new[] { "COFFEE", "QAHVA" } },
        { "label.plates", new[] { "PLATES", "TARELKALAR" } },
        { "label.bags", new[] { "BAGS", "PAKETLAR" } },
        { "label.extinguisher", new[] { "FIRE EXTINGUISHER", "O'T O'CHIRGICH" } },
        { "trash.plate", new[] { "Plates don't go in the trash — wash them in the sink", "Tarelka tashlanmaydi — uni rakovinada yuving" } },

        // Tarelka va paket
        { "plate.put", new[] { "Put on a plate: {0}", "Tarelkaga qo'yish: {0}" } },
        { "bag.put", new[] { "Put in a bag: {0}", "Paketga solish: {0}" } },
        { "plate.remove", new[] { "Take it off the plate", "Tarelkadan olish" } },
        { "bag.remove", new[] { "Take it out of the bag", "Paketdan chiqarish" } },
        { "plate.info", new[] { "Clean plates: {0} — for eat-in customers", "Toza tarelkalar: {0} — shu yerda yeydiganlar uchun" } },
        { "bag.info", new[] { "Paper bags: {0} — for take-away customers", "Qog'oz paketlar: {0} — olib ketadiganlar uchun" } },
        { "plate.washFirst", new[] { "Wash the dirty plate in the sink first", "Iflos tarelkani avval rakovinada yuving" } },
        { "plate.none", new[] { "No clean plates left — wash the dirty ones in the sink!", "Toza tarelka qolmadi — iflaslarini rakovinada yuving!" } },
        { "bag.none", new[] { "Out of paper bags! Order more on the computer.", "Qog'oz paket tugadi! Kompyuterdan buyurtma bering." } },
        { "container.notNeeded", new[] { "{0} doesn't need a plate or a bag", "{0} uchun tarelka ham, paket ham kerak emas" } },
        { "container.onlyFood", new[] { "Only a finished burger or lavash goes here", "Bu yerga faqat tayyor burger yoki lavash qo'yiladi" } },
        { "container.already", new[] { "It's already packed", "U allaqachon solingan" } },

        // Rakovina, qahva, muzlatgich
        { "sink.wash", new[] { "Wash the plate", "Tarelkani yuvish" } },
        { "sink.busy", new[] { "Washing a plate...", "Tarelka yuvilmoqda..." } },
        { "sink.info", new[] { "Sink: bring dirty plates here", "Rakovina: iflos tarelkalarni shu yerga olib keling" } },
        { "sink.onlyPlates", new[] { "Only dirty plates are washed here", "Bu yerda faqat iflos tarelkalar yuviladi" } },
        { "coffee.brew", new[] { "Make a coffee", "Qahva tayyorlash" } },
        { "coffee.brewing", new[] { "Making coffee...", "Qahva tayyorlanmoqda..." } },
        { "coffee.noBeans", new[] { "Out of coffee beans — order more on the computer", "Qahva doni tugagan — kompyuterdan buyurtma bering" } },
        { "coffee.info", new[] { "Coffee machine — beans for {0} cups", "Qahva mashinasi — {0} stakanga don bor" } },
        { "fridge.open", new[] { "Open the fridge", "Muzlatgichni ochish" } },
        { "fridge.close", new[] { "Close the fridge", "Muzlatgichni yopish" } },
        { "fridge.info", new[] { "Fridge: patties and meat. Don't leave it open!", "Muzlatgich: kotlet va go'sht. Ochiq qoldirmang!" } },
        { "fridge.spoiled", new[] { "{0} went bad in the open fridge! Close the door!", "Ochiq muzlatgichda {0} aynib qoldi! Eshikni yoping!" } },
        { "fridge.noSpoiledBack", new[] { "Spoiled food goes in the trash, not back in the fridge", "Aynigan mahsulot muzlatgichga emas, axlatga tashlanadi" } },
        { "bin.spoiled", new[] { "{0} spoiled", "{0} ta aynigan" } },
        { "bin.inStockSpoiled", new[] { "In stock: {0} — {1} spoiled, take them out and throw them away", "Zaxirada: {0} ta — {1} tasi aynigan, olib axlatga tashlang" } },

        // Yong'in
        { "fire.alert", new[] { "FIRE on the grill! Grab the fire extinguisher!", "Grilda YONG'IN! O't o'chirgichni oling!" } },
        { "fire.putOut", new[] { "Put out the fire", "Olovni o'chirish" } },
        { "fire.nothing", new[] { "Nothing is burning", "Hech narsa yonayotgani yo'q" } },
        { "fire.out", new[] { "The fire is out. Throw the burnt food in the trash.", "Olov o'chdi. Kuygan narsani axlatga tashlang." } },
        { "fire.tooHot", new[] { "It's on fire — put it out with the extinguisher first!", "U yonmoqda — avval o't o'chirgich bilan o'chiring!" } },

        // Yig'ish taxtasi
        { "board.giveProduct", new[] { "Give the finished food to a customer", "Tayyor mahsulotni mijozga bering" } },
        { "board.needBase", new[] { "Start with a burger bun or lavash bread", "Avval burger noni yoki lavash noni qo'ying" } },
        { "board.addBeforeClosing", new[] { "Add before closing: {0}", "Yopishdan oldin qo'shing: {0}" } },
        { "board.notForBurger", new[] { "A burger doesn't need {0}", "Burgerga {0} kerak emas" } },
        { "board.notForLavash", new[] { "A lavash doesn't need {0}", "Lavashga {0} kerak emas" } },
        { "board.alreadyAdded", new[] { "{0} is already added", "{0} allaqachon qo'shilgan" } },
        { "board.raw", new[] { "This is still raw — cook it on the grill first!", "Bu hali xom — avval grilda pishiring!" } },
        { "board.burnt", new[] { "This is burnt — throw it away!", "Bu kuygan — axlatga tashlang!" } },
        { "board.empty", new[] { "Assembly board: start with a burger bun or lavash bread", "Yig'ish taxtasi: burger noni yoki lavash noni qo'ying" } },
        { "board.burgerReady", new[] { "Burger: now close it with a bun", "Burger: endi ustiga non qo'yib yoping" } },
        { "board.lavashReady", new[] { "Lavash: everything's in — roll it!", "Lavash: hammasi bor — o'rang!" } },
        { "board.burgerNeeds", new[] { "Burger needs: {0}", "Burger uchun kerak: {0}" } },
        { "board.lavashNeeds", new[] { "Lavash needs: {0}", "Lavash uchun kerak: {0}" } },

        // Mijozlar
        { "customer.coming", new[] { "Customer is coming...", "Mijoz kelmoqda..." } },
        { "customer.orderTakeAway", new[] { "To go: {0}", "Olib ketadi: {0}" } },
        { "customer.orderDineIn", new[] { "Eating here: {0}", "Shu yerda yeydi: {0}" } },
        { "customer.eating", new[] { "Enjoying the meal...", "Ovqatlanmoqda..." } },
        { "customer.wantPlate", new[] { "I'm eating here — on a plate, please!", "Men shu yerda yeyman — tarelkada bering!" } },
        { "customer.wantBag", new[] { "It's to go — put it in a paper bag!", "Olib ketaman — qog'oz paketga soling!" } },
        { "customer.notAsked", new[] { "I didn't ask for this!", "Men buni so'ramadim!" } },
        { "customer.notPressed", new[] { "The lavash isn't pressed yet!", "Lavash hali presslanmagan!" } },
        { "customer.burnt", new[] { "This is burnt!", "Bu kuyib ketgan-ku!" } },
        { "customer.notOrdered", new[] { "I didn't order a {0}!", "Men {0} buyurtma qilmaganman!" } },
        { "customer.stillNeed", new[] { "Thanks! I still need: {0}", "Rahmat! Yana kerak: {0}" } },

        // Xabarlar
        { "msg.handsFull", new[] { "Your hands are full — put down what you're holding first", "Qo'lingiz band — avval qo'ldagini qo'ying" } },
        { "msg.closingTime", new[] { "21:00 — working hours are over, no new customers. Close the shop at the sign when you're done.", "Soat 21:00 — ish vaqti tugadi, yangi mijoz kelmaydi. Ishlarni tugatgach, tablodan do'konni yoping." } },
        { "msg.reopened", new[] { "The shop is open again!", "Do'kon yana ochildi!" } },
        { "msg.break", new[] { "Closed for a break — no new customers will come", "Do'kon tanaffusga yopildi — yangi mijoz kelmaydi" } },
        { "msg.customersInside", new[] { "Customers are still inside — serve them first", "Ichkarida hali mijozlar bor — avval ularga xizmat qiling" } },
        { "msg.opened", new[] { "The shop is open! Customers are coming.", "Do'kon ochildi! Mijozlar kelmoqda." } },
        { "msg.tip", new[] { "(tip {0})", "(choychaqa {0})" } },
        { "msg.left", new[] { "Left!", "Ketib qoldi!" } },
        { "msg.customerLeft", new[] { "A customer got tired of waiting and left!", "Mijoz kutib-kutib ketib qoldi!" } },
        { "msg.noMoney", new[] { "Not enough money!", "Pul yetarli emas!" } },
        { "msg.installed", new[] { "{0} — installed!", "{0} — o'rnatildi!" } },

        // Ekran (HUD)
        { "hud.day", new[] { "Day {0}   |   {1}", "Kun {0}   |   {1}" } },
        { "hud.phase.Prep", new[] { "Preparation — shop closed", "Tayyorgarlik — do'kon yopiq" } },
        { "hud.phase.Open", new[] { "Shop open", "Do'kon ochiq" } },
        { "hud.phase.Break", new[] { "Break — shop closed", "Tanaffus — do'kon yopiq" } },
        { "hud.phase.AfterHours", new[] { "Working hours are over", "Ish vaqti tugadi" } },
        { "hud.helpHint", new[] { "(H — help)", "(H — yordam)" } },
        { "hud.stock", new[] { "Stock", "Zaxira" } },
        { "hud.prepBanner", new[] { "Get ready, then go to the  START  sign by the counter and press E", "Tayyorlaning, so'ng kassa yonidagi  OCHISH  tablosiga borib E bosing" } },
        { "hud.breakBanner", new[] { "Closed for a break — open the shop again at the sign by the counter", "Tanaffus — do'konni kassa yonidagi tablodan qayta oching" } },
        { "hud.afterHoursBanner", new[] { "Working hours are over. Finish up, then close the shop at the sign to end the day", "Ish vaqti tugadi. Ishlarni tugatib, tablodan do'konni yoping — kun yakunlanadi" } },
        { "hud.afterHoursBusy", new[] { "Working hours are over — serve the remaining customers", "Ish vaqti tugadi — qolgan mijozlarga xizmat qiling" } },
        { "hud.fridgeOpen", new[] { "The fridge is open — close it or the meat will spoil!", "Muzlatgich ochiq qoldi — yoping, aks holda go'sht aynib qoladi!" } },
        { "hud.fridgeSpoiling", new[] { "The meat is spoiling — close the fridge!", "Go'sht aynimoqda — muzlatgichni yoping!" } },
        { "hud.spoiledCount", new[] { "{0} bad", "{0} aynigan" } },
        { "hud.holding", new[] { "Holding: {0}", "Qo'lda: {0}" } },
        { "hud.ready", new[] { "READY!", "TAYYOR!" } },
        { "hud.burnt", new[] { "BURNT!", "KUYDI!" } },
        { "hud.fireTag", new[] { "FIRE!", "YONMOQDA!" } },
        { "hud.fire", new[] { "FIRE! Take the extinguisher next to the grill and press E on the grill", "YONG'IN! Gril yonidagi o't o'chirgichni olib, grilga E bosing" } },
        { "hud.washing", new[] { "Washing...", "Yuvilmoqda..." } },
        { "hud.plates", new[] { "Clean plates", "Toza tarelkalar" } },
        { "hud.toGo", new[] { "TO GO", "OLIB KETADI" } },
        { "hud.eatIn", new[] { "EATING HERE", "SHU YERDA" } },
        { "help.title", new[] { "How to play", "Qanday o'ynash kerak" } },
        { "help.body", new[]
            {
                "<b><color=#ffb347>BURGER</color></b>\n" +
                "Burger bun > cooked Patty > Cheese > Tomato > another Burger bun (close)\n\n" +
                "<b><color=#ffe999>LAVASH</color></b>\n" +
                "Lavash bread > grilled Meat > Tomato > Sauce > E with empty hands (roll) > heat it in the press\n\n" +
                "<b><color=#d9a877>COFFEE</color></b>\n" +
                "Press E on the coffee machine (uses coffee beans), wait and take the cup.\n\n" +
                "<b><color=#8fd3ff>EATING HERE or TO GO</color></b>\n" +
                "Customers at a table get their food on a plate. When they leave, take the dirty plate to the sink and wash it.\n" +
                "Customers at the counter take it away — put the food in a paper bag.\n\n" +
                "Patties and meat are in the fridge — <color=#ff6655>close it, or the meat spoils.</color> Don't forget them on the grill — " +
                "<color=#ff6655>they burn and catch fire!</color> Put the fire out with the extinguisher and throw the leftovers in the trash.\n" +
                "Open and close the shop at the sign by the counter whenever you like (the clock keeps running). " +
                "After 21:00 no new customers come: finish up, then close the shop to end the day.\n\n" +
                "<color=#cccccc>WASD — move, Shift — run, Mouse — look\n" +
                "E / left click — pick up, put down, use\n" +
                "H — close help, Esc — pause</color>",

                "<b><color=#ffb347>BURGER</color></b>\n" +
                "Burger noni > pishgan Kotlet > Pishloq > Pomidor > yana Burger noni (yopish)\n\n" +
                "<b><color=#ffe999>LAVASH</color></b>\n" +
                "Lavash noni > qovurilgan Go'sht > Pomidor > Sous > bo'sh qo'l bilan E (o'rash) > pressda qizdirish\n\n" +
                "<b><color=#d9a877>QAHVA</color></b>\n" +
                "Qahva mashinasiga E bosing (qahva doni ketadi), kuting va stakanni oling.\n\n" +
                "<b><color=#8fd3ff>SHU YERDA yoki OLIB KETISH</color></b>\n" +
                "Stolda o'tirgan mijozga taom tarelkada beriladi. U ketgach, iflos tarelkani rakovinaga olib borib yuving.\n" +
                "Kassadagi mijoz olib ketadi — taomni qog'oz paketga soling.\n\n" +
                "Kotlet va go'sht muzlatgichda — <color=#ff6655>eshigini yoping, aks holda aynaydi.</color> Grilda unutib qo'ymang — " +
                "<color=#ff6655>kuyib, yonib ketadi!</color> Olovni o't o'chirgich bilan o'chiring, qolganini axlatga tashlang.\n" +
                "Do'konni kassa yonidagi tablodan istalgan payt oching va yoping (soat yuraveradi). " +
                "21:00 dan keyin yangi mijoz kelmaydi: ishlarni tugatib, do'konni yoping — kun yakunlanadi.\n\n" +
                "<color=#cccccc>WASD — yurish, Shift — yugurish, Sichqoncha — qarash\n" +
                "E / chap tugma — olish, qo'yish, ishlatish\n" +
                "H — yordamni yopish, Esc — pauza</color>"
            }
        },

        // Menyular
        { "menu.continue", new[] { "Continue (Day {0})", "Davom ettirish (Kun {0})" } },
        { "menu.confirmNew", new[] { "Sure? This deletes your save", "Rostdan ham? Eski o'yin o'chadi" } },
        { "menu.newGame", new[] { "New game", "Yangi o'yin" } },
        { "menu.quit", new[] { "Quit", "Chiqish" } },
        { "menu.footer", new[] { "WASD — move  |  E — interact  |  H — help  |  Esc — pause", "WASD — yurish  |  E — olish  |  H — yordam  |  Esc — pauza" } },
        { "settings.sensitivity", new[] { "Mouse sensitivity: {0}", "Sichqoncha sezgirligi: {0}" } },
        { "settings.volume", new[] { "Volume: {0}%", "Ovoz: {0}%" } },
        { "settings.language", new[] { "Language", "Til" } },
        { "pause.title", new[] { "Paused", "Pauza" } },
        { "pause.resume", new[] { "Resume", "Davom etish" } },
        { "pause.mainMenu", new[] { "Main menu", "Bosh menyu" } },
        { "pause.notSaved", new[] { "Today's progress won't be saved", "Bugungi o'yin saqlanmaydi" } },
        { "pause.quit", new[] { "Quit game", "O'yindan chiqish" } },
        { "shop.title", new[] { "Shop computer", "Do'kon kompyuteri" } },
        { "shop.ingredients", new[] { "Ingredients", "Ingredientlar" } },
        { "shop.supplies", new[] { "Supplies", "Jihozlar" } },
        { "shop.upgrades", new[] { "Upgrades", "Yaxshilanishlar" } },
        { "shop.owned", new[] { "Purchased", "Sotib olingan" } },
        { "shop.close", new[] { "Close (Esc)", "Yopish (Esc)" } },
        { "summary.title", new[] { "Day {0} complete", "Kun {0} yakunlandi" } },
        { "summary.served", new[] { "Served:  {0} customers", "Xizmat ko'rsatildi:  {0} mijoz" } },
        { "summary.lost", new[] { "Walked out:  {0} customers", "Ketib qolganlar:  {0} mijoz" } },
        { "summary.revenue", new[] { "Revenue:  {0}", "Tushum:  {0}" } },
        { "summary.tips", new[] { "Tips:  {0}", "Choychaqa:  {0}" } },
        { "summary.purchases", new[] { "Purchases:  {0}", "Xaridlar:  {0}" } },
        { "summary.rent", new[] { "Rent:  -{0}", "Ijara:  -{0}" } },
        { "summary.profit", new[] { "Day profit", "Kun foydasi" } },
        { "summary.balance", new[] { "Balance", "Hisobda" } },
        { "summary.saved", new[] { "Game saved automatically", "O'yin avtomatik saqlandi" } },
        { "summary.next", new[] { "Next day  (Day {0})", "Keyingi kun  (Kun {0})" } },
        { "over.title", new[] { "BANKRUPT!", "BANKROT!" } },
        { "over.rent", new[] { "You couldn't pay the rent.", "Ijarani to'lashga pul yetmadi." } },
        { "over.days", new[] { "You lasted {0} days.", "Siz {0} kun ishladingiz." } },
        { "over.served", new[] { "Total served:  {0} customers", "Jami xizmat ko'rsatildi:  {0} mijoz" } },
        { "over.balance", new[] { "Balance:  {0}", "Hisob:  {0}" } }
    };
}
