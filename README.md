# Lavash & Burger Simulator

Birinchi shaxs ko'rinishidagi fast food simulyatori. Siz kichik lavash va burger do'konini boshqarasiz: ovqat pishirasiz, mijozlarga xizmat qilasiz, pul yig'asiz va do'konni kengaytirasiz.

## Ishga tushirish

**Unity'siz o'ynash:** `Build\LavashBurger.exe` ni ikki marta bosing.

**Unity'da ochish (Unity 6000.6.7f1 da sinalgan):**
1. Unity Hub'da **Add → Add project from disk** ni bosib, shu `game` papkasini tanlang.
2. Loyihani oching va `Assets/Scenes/Game.unity` sahnasini oching (odatda o'zi ochiladi).
3. **Play** tugmasini bosing.

> Sahna yo'qolsa: yuqori menyudan **Lavash Burger → O'yin sahnasini qayta yaratish** ni bosing.
> Istalgan bo'sh sahnada Play bosilganda ham o'yin o'zi quriladi.

**Yangi .exe yasash:** File → Build Profiles → Windows → Build.
`Assets/Resources` dagi materiallarni (`BaseMaterial`, `GlowMaterial`, `ParticleMaterial`, `WorldTextMaterial`) o'chirmang: busiz tayyor .exe da shaderlar build'ga kirmay qoladi va narsalar pushti rangda chiqadi. Ular o'chib ketsa, Unity'ni qayta ochganda avtomatik qayta yaratiladi.

## Til

O'yin standart holatda **ingliz tilida**. O'zbekchaga o'tish uchun bosh menyu yoki pauza (Esc) → **Language** → **O'zbekcha** ni bosing. Til darhol almashadi va eslab qolinadi.

Barcha matnlar bitta faylda: `Assets/Scripts/Core/Loc.cs`. Har bir matnning inglizcha va o'zbekcha varianti bor.
- **Matnni o'zgartirish:** jadvaldagi tarjimani tahrirlang.
- **Yangi til qo'shish (masalan, rus tili):**
  1. `Language` ro'yxatiga yangi til nomini qo'shing.
  2. Jadvaldagi har bir qatorga uchinchi tarjimani yozing.
  3. Sozlamalarga yangi tugma qo'shing.

## Boshqaruv

| Tugma | Vazifasi |
|---|---|
| WASD | Yurish |
| Shift | Yugurish |
| Sichqoncha | Atrofga qarash |
| E yoki chap tugma | Olish, qo'yish, ishlatish, mijozga berish |
| H | Yordam oynasi |
| Esc | Pauza va sozlamalar |

## Retseptlar

**Burger** ($8):
burger noni → pishgan kotlet → pishloq → pomidor → yana burger noni (yopish).

**Lavash** ($9):
lavash noni → qovurilgan go'sht → pomidor → sous → bo'sh qo'l bilan E (o'rash) → pressda qizdirish.

**Qahva** ($3):
qahva mashinasiga E bosing (bitta porsiya qahva doni ketadi), 5 soniya kuting va stakanni oling.

Ichidagi ingredientlarni istalgan tartibda qo'yish mumkin. Kotlet va go'sht **muzlatgichda** turadi: eshigiga qarab E bosing, eshik ochiladi, keyin javondan oling. Ikkala eshik alohida ochiladi va yopiladi.

**Muzlatgichni ochiq qoldirmang.** Eshik 10 soniyadan ko'p ochiq tursa, muzlatgich pipillaydi va ekranda ogohlantirish chiqadi. 25 soniyadan keyin eshik yopilguncha har 3 soniyada bitta kotlet yoki go'sht aynaydi. Aynigan mahsulot ko'kimtir rangda bo'ladi, muzlatgichda qoladi va olinganda birinchi bo'lib chiqadi. Uni grilga ham, taxtaga ham qo'yib bo'lmaydi — axlatga tashlang. Zaxira panelida va eshikdagi yozuvda nechtasi aynigani ko'rinadi.

## Shu yerda yeyish yoki olib ketish

- **Olib ketadigan mijoz** kassa oldida turadi. Burger va lavashni **qog'oz paketga** solib bering: tayyor taomni ushlab, orolchadagi PAKETLAR joyiga E bosing.
- **Shu yerda yeydigan mijoz** zaldagi stolga o'tirib buyurtma beradi. Taomni **tarelkaga** qo'yib (TARELKALAR joyi), stolga olib borib bering. Mijoz to'laydi, ovqatlanadi va ketgach stolda **iflos tarelka** qoladi.
- Iflos tarelkani **rakovinaga** olib borib yuving (3 soniya). Restoranda 6 ta tarelka bor, ular sotib olinmaydi — faqat yuviladi. Har kuni ertalab hammasi toza bo'ladi.
- Ustida iflos tarelka yoki qolgan taom turgan stolga yangi mijoz o'tirmaydi.
- Qahva o'z stakanida beriladi, unga tarelka ham, paket ham kerak emas.
- Mijoz boshi ustidagi yozuv kimligini ko'rsatadi: **TO GO / OLIB KETADI** yoki **EATING HERE / SHU YERDA**. Noto'g'ri idishda bersangiz, mijoz olmaydi.
- Tarelkadagi taomni axlatga tashlasangiz, taom tashlanadi, tarelka esa qo'lingizda iflos bo'lib qoladi.

## Yong'in

Pishgan go'shtni grilda unutib qo'ysangiz, avval **kuyadi**, yana bir necha soniyadan keyin **yonib ketadi**. Yong'in paytida:
- ekranda qizil ogohlantirish chiqadi va signal chalinadi;
- yonayotgan narsani olib bo'lmaydi;
- mijozlar tutundan bezovta bo'lib, ikki baravar tez sabrsizlanadi.

Gril yonidagi stenddan **o't o'chirgichni** oling va grilga qarab E bosing. O't o'chirgich bir martalik — keyingisini kompyuterdan sotib oling. Olov o'chgach, kuygan qoldiqni olib axlatga tashlang.

## Mijozlar

Mijozlar har xil: **bolalar** (past bo'yli, tez yuradi, sabri kamroq, qahva ichmaydi), **kattalar** va **keksalar** (sekin yuradi, lekin sabrliroq). Kenney odamchalari juda katta boshli bo'lgani uchun suyaklari animatsiyadan keyin cho'ziladi: kattalarda oyoq va tana uzunroq, bosh kichikroq (`CharacterShape`).

## O'yin sikli

1. **Tayyorgarlik.** Soat 09:00 da to'xtab turadi. Zaxirani tekshirasiz, kerak bo'lsa kompyuterdan ingredient, qog'oz paket, qahva doni, o't o'chirgich yoki yaxshilanish sotib olasiz.
2. **Do'konni ochish.** Kassa yonidagi tablo (OCHISH) ga E bosasiz. Shundan keyin soat yura boshlaydi: 09:00 dan 21:00 gacha (real vaqtda taxminan 5 daqiqa).
3. **Xizmat.** Har bir mijoz boshi ustida buyurtmasi va sabr chizig'i ko'rinadi. Tez bersangiz choychaqa ko'proq bo'ladi. Sabri tugasa, mijoz ketib qoladi.
4. **Tanaffus.** Ish vaqtida tablodan do'konni istalgan payt yopib, keyin qayta ochishingiz mumkin (masalan, tushlikda yoki ishlar to'planib qolganda). Yopiq paytda yangi mijoz kelmaydi, ichkaridagilarga esa xizmat qilishda davom etasiz. Soat tanaffusda ham yuraveradi.
5. **Ish vaqti tugashi.** 21:00 da yangi mijozlar kelmay qo'yadi, lekin kun o'zi tugamaydi: qolgan mijozlarga xizmat qilasiz, idishlarni yuvasiz, ertangi kunga zakaz berasiz. Tayyor bo'lgach, tablodan **do'konni yopasiz** — shunda kun yakunlanadi. Ichkarida mijoz qolgan bo'lsa, yopib bo'lmaydi.
6. **Kun yakuni.** Hisobot chiqadi: tushum, choychaqa, xaridlar, ijara. O'yin avtomatik saqlanadi.
7. Ijarani to'lashga pul yetmasa, **bankrot** bo'lasiz.

Kunlar o'tgan sari mijozlar ko'payadi, sabri kamayadi, ijara oshadi. 3-kundan boshlab ba'zi mijozlar 2 ta taom buyuradi. Ba'zilar taom bilan qahva oladi, ba'zilar faqat qahva ichgani kiradi.

**Yaxshilanishlar:** gril +2 joy, kuchli olov (1.5 baravar tez), ikkinchi press, uchinchi taxta, reklama (mijozlar tez-tez keladi), shinam dekor (mijozlar uzoqroq kutadi).

## Balansni o'zgartirish

Barcha narxlar, pishirish vaqtlari, kun uzunligi, ijara va yaxshilanishlar bitta faylda:
`Assets/Scripts/Core/GameData.cs`.

## Loyiha tuzilmasi

Restoran kod orqali quriladi (`WorldBuilder`), sahnada faqat bitta `Game` obyekti bor.

**Grafika:**
- **URP render pipeline:** yumshoq soyalar, SSAO, bloom, rang tuzatish va vinyetka.
- **3D modellar:** Kenney (CC0) to'plamlari:
  - ovqatlar: burger, kotlet, pishloq, pomidor, ketchup;
  - oshxona mebellari va zal jihozlari;
  - animatsiyali odamchalar.
- **Kod bilan yasalgan shakllar (`MeshGen`):** burger noni (kunjutli), lavash, press, laganlar, devor va pol.
- **Kod bilan yasalgan teksturalar (`TextureGen`):** kafel, yog'och pol, plitka devor.
- **Effektlar:** grilda bug' va tutun, yong'in alangasi va o't o'chirgich ko'pigi, rakovinada suv, burger tayyor bo'lganda uchqunlar, to'lovda tangalar.
- **Muzlatgich:** kod bilan yasalgan, ikki eshigi ochiladi, ichida chiroq yonadi.
- **Ikonchalar:** UI'dagi ovqat ikonchalari o'yin boshida 3D modellardan avtomatik yaratiladi (`IconRenderer`).
- **Tovushlar:** kodda sintez qilinadi.

```
Assets/Scripts/
  Core/       GameManager (o'yin oqimi), WorldBuilder (restoranni quradi), GameData (balans),
              SaveData (saqlash), SoundManager (tovushlar), GameRoot (ishga tushirish),
              ModelLib (3D modellar), MeshGen (yumaloq shakllar), TextureGen (teksturalar),
              Effects (post-processing, zarrachalar), Util
  Player/     PlayerController (yurish), PlayerInteractor (olish/qo'yish), GameInput
  Items/      Item (ovqat, pishish holati), ItemFactory (ovqat modellari), Interactable
  Stations/   CookStation (gril, press, yong'in), AssemblyBoard (yig'ish taxtasi), IngredientBin,
              Fridge (muzlatgich), Sink (rakovina), CoffeeMachine, ContainerStation (tarelka/paket),
              TrashBin, ShopComputer, OpenSign
  Customers/  Customer (olib ketadigan va stolda yeydigan mijoz), CustomerManager (joylar, o'rindiqlar),
              CharacterShape (mijoz bo'yi va proporsiyasi)
  UI/         GameUI (ekran, menyular, do'kon), IconRenderer (ovqat ikonchalari)
Assets/Shaders/WorldText.shader  3D yozuvlar devor orqali ko'rinmasligi uchun
Assets/Resources/Models/         Kenney modellari (Food, Furniture, Characters)
Assets/Settings/                 URP sozlamalari
Assets/Editor/                   sahna va materiallarni avtomatik yaratish, model importi
```

## Keyingi qadamlar (g'oyalar)

- Fon musiqasi va ko'proq tovushlar.
- O'yinchining qo'llari (ushlab turish animatsiyasi).
- Yangi mahsulotlar: `GameData.cs` dagi `ItemType` ga qo'shib, retseptini yozish kifoya.
- Ishchi yollash, sovuq ichimliklar, kunlik maqsadlar.
- Steam sahifasi va bepul demo.

## Mualliflik

3D modellar: **Kenney** (www.kenney.nl). To'plamlar: Food Kit, Furniture Kit, Mini Characters. Litsenziya: Creative Commons CC0 (bepul, tijoriy o'yinda ham ishlatish mumkin). Litsenziya fayllari `Assets/Resources/Models/` ichida.
