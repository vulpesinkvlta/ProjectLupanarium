"""Initial translation seed. The editable source of truth is translations.json after generation."""
import json, re
from pathlib import Path

data = r'''+1 мурмиллон в отряд до конца забега.|+1 murmillo in your squad for this run.|Bu koşu boyunca ekibe +1 murmillo.
+1,5 брони; −15% скорости, +8% времени перезарядки.|+1.5 armor; −15% speed, +8% attack cooldown.|+1,5 zırh; −%15 hız, +%8 saldırı bekleme süresi.
+10% здоровья всему отряду до конца забега.|+10% health to the whole squad for this run.|Bu koşu boyunca tüm ekibe +%10 can.
+10% здоровья, −5% урона.|+10% health, −5% damage.|+%10 can, −%5 hasar.
+12% урона, −8% здоровья.|+12% damage, −8% health.|+%12 hasar, −%8 can.
+15% дальности, +5% урона, −15% скорости.|+15% range, +5% damage, −15% speed.|+%15 menzil, +%5 hasar, −%15 hız.
+15% скорости, −5% здоровья.|+15% speed, −5% health.|+%15 hız, −%5 can.
+8% урона всему отряду до конца забега.|+8% damage to the whole squad for this run.|Bu koşu boyunca tüm ekibe +%8 hasar.
−8% перезарядки, +8% скорости, −0,5 брони.|−8% attack cooldown, +8% speed, −0.5 armor.|−%8 saldırı bekleme süresi, +%8 hız, −0,5 zırh.
01   ГЛАДИАТОРЫ|01   GLADIATORS|01   GLADYATÖRLER
02   ПОСТРОЙКИ|02   BUILDINGS|02   BİNALAR
03   АРСЕНАЛ|03   ARMORY|03   CEPHANELİK
100 урона с учётом брони всем в радиусе 1,6; скорость −35% на 4 секунды. Задевает своих. Один заряд.|Deals 100 damage, reduced by armor, to everyone within 1.6 units; −35% speed for 4 seconds. Also hits allies. One charge.|1,6 birim içindeki herkese zırha göre azalan 100 hasar verir; 4 saniyeliğine −%35 hız. Müttefikleri de etkiler. Tek yük.
Бесплатный отдых: 2:00|Free rest: 2:00|Ücretsiz dinlenme: 2:00
Благословление|Blessing|Lütuf
Благословление Императора|Emperor's Blessing|İmparatorun Lütfu
Благословления|Blessings|Lütuflar
Блокировано|Blocked|Engellenen
Бонусы и восстановление|Bonuses and recovery|Bonuslar ve iyileşme
Бронированные секуторы с оглушением.|Armored secutors with a stunning strike.|Sersemletme yeteneğine sahip zırhlı secutorlar.
Будут удалены забег, валюта, открытые юниты и формации, постройки, снаряжение и благословления.\nЭто действие нельзя отменить.|Your run, currency, unlocked units and formations, buildings, equipment and blessings will be deleted.\nThis cannot be undone.|Koşunuz, paranız, açılan birimler ve dizilişler, binalar, ekipmanlar ve lütuflar silinecek.\nBu işlem geri alınamaz.
В бой|Fight|Savaş
В лупанарий|To base|Üsse dön
ВАШ ОТРЯД|YOUR SQUAD|EKİBİNİZ
Волк|Wolf|Kurt
Волчья стая|Wolf pack|Kurt sürüsü
Восстанавливает 150 здоровья живым бойцам в радиусе 1,6, включая врагов. Один заряд.|Restores 150 health to living fighters within 1.6 units, including enemies. One charge.|1,6 birim içindeki hayatta olan savaşçılara 150 can kazandırır. Düşmanlar da etkilenir. Tek yük.
Всем гладиаторам +1 брони; складывается с классовым снаряжением.|+1 armor to all gladiators; stacks with class equipment.|Tüm gladyatörlere +1 zırh; sınıf ekipmanıyla birlikte uygulanır.
Всем гладиаторам +8% здоровья.|+8% health to all gladiators.|Tüm gladyatörlere +%8 can.
Выберите благословление, затем место на арене|Choose a blessing, then a spot in the arena|Bir lütuf, ardından arenada bir konum seçin
Выберите контракт|Choose a contract|Bir sözleşme seçin
Выберите строй перед боем|Choose a formation before battle|Savaştan önce bir diziliş seçin
Выбрать|Select|Seç
Выбрать контракт|Select contract|Sözleşmeyi seç
Выжило|Survived|Sağ kalan
Выносливые звери с сильным ударом.|Tough beasts with a powerful strike.|Güçlü vuruşları olan dayanıklı hayvanlar.
Гимназия|Gymnasium|Eğitim alanı
Гладиатор|Gladiator|Gladyatör
ГЛАДИАТОРСКАЯ ШКОЛА|GLADIATOR SCHOOL|GLADYATÖR OKULU
Гладиаторы|Gladiators|Gladyatörler
Гладиус|Gladius|Gladius
Гопломах|Hoplomachus|Hoplomachus
Денарии: 0|Denarii: 0|Denarius: 0
Дополнительный бонус за просмотр|Extra reward for watching an ad|Reklam izleyerek ek ödül
Закрыть|Close|Kapat
Замедляет цель на 35% на 2 секунды.|Slows the target by 35% for 2 seconds.|Hedefi 2 saniyeliğine %35 yavaşlatır.
Заточка оружия|Weapon sharpening|Silah bileme
Здесь будет отдыхать ваш отряд\n<color=#806F55><size=19>Начните забег и выберите бойцов</size></color>|Your squad will rest here\n<color=#806F55><size=19>Start a run and choose your fighters</size></color>|Ekibiniz burada dinlenecek\n<color=#806F55><size=19>Bir koşu başlatın ve savaşçılarınızı seçin</size></color>
Изменить строй|Change formation|Dizilişi değiştir
Императорский пыл|Imperial fervor|İmparatorluk coşkusu
Итоги боя|Battle results|Savaş sonuçları
К награде|Claim reward|Ödüle geç
Каждый уровень: +0,5 брони и +2 процентных пункта шанса крита всем гладиаторам.|Each level: +0.5 armor and +2 percentage points of critical chance to all gladiators.|Her seviye: tüm gladyatörlere +0,5 zırh ve +2 yüzde puan kritik vuruş şansı.
Каждый уровень: +10% максимального здоровья всем гладиаторам.|Each level: +10% maximum health to all gladiators.|Her seviye: tüm gladyatörlere +%10 azami can.
Каждый уровень: +8% урона всем гладиаторам.|Each level: +8% damage to all gladiators.|Her seviye: tüm gladyatörlere +%8 hasar.
Каждый уровень: ретиариям +0,1 дальности и −3% перезарядки атаки.|Each level: +0.1 range and −3% attack cooldown for retiarii.|Her seviye: retiariuslara +0,1 menzil ve −%3 saldırı bekleme süresi.
Кипящая смола|Boiling tar|Kaynar katran
Класс|Class|Sınıf
Клин|Wedge|Kama
Колонна|Column|Kol
Круг|Circle|Çember
Кузница|Forge|Demirhane
Купить|Buy|Satın al
Лазарет|Infirmary|Revir
Лев|Lion|Aslan
Лорика|Lorica|Lorica
ЛУПАНАРИЙ|LUPANARIUM|LUPANARIUM
Львы арены|Lions of the arena|Arena aslanları
Манипула|Maniple|Manipül
Много быстрых противников без брони.|Many fast, unarmored enemies.|Çok sayıda hızlı ve zırhsız düşman.
Мурмиллон|Murmillo|Murmillo
Мурмиллонам +1 брони и +5% здоровья.|+1 armor and +5% health to murmillos.|Murmillolara +1 zırh ve +%5 can.
Мурмиллонам +10% урона.|+10% damage to murmillos.|Murmillolara +%10 hasar.
На базу|To base|Üsse dön
Наберите отряд|Recruit a squad|Bir ekip kurun
Награда за победу|Victory reward|Zafer ödülü
Название|Name|Ad
Нанесено|Dealt|Verilen
Нанять отряд|Hire squad|Ekibi tut
Настройки|Settings|Ayarlar
Настройки появятся здесь позже.|Settings will be added here later.|Ayarlar daha sonra buraya eklenecek.
Начать забег|Start run|Koşuyu başlat
Начать новый забег|Start new run|Yeni koşu başlat
Небольшой отряд сбалансированных бойцов.|A small squad of well-rounded fighters.|Dengeli savaşçılardan oluşan küçük bir ekip.
Нет активного забега|No active run|Aktif koşu yok
Оглушает цель на 0,45 секунды.|Stuns the target for 0.45 seconds.|Hedefi 0,45 saniyeliğine sersemletir.
Описание|Description|Açıklama
Открыть|Unlock|Kilidi aç
Отмена|Cancel|İptal
ОТРЯД ЕЩЁ НЕ СОБРАН|NO SQUAD YET|HENÜZ EKİP YOK
Первые соперники|First rivals|İlk rakipler
Пилум|Pilum|Pilum
Полный сброс прогресса|Reset all progress|Tüm ilerlemeyi sıfırla
Получено|Taken|Alınan
Получить|Claim|Al
Пополнение|Reinforcements|Takviye
Поска|Posca|Posca
Преследователи|Pursuers|Takipçiler
Продолжить без строя|Continue without formation|Dizilişsiz devam et
ПРОТИВНИК|ENEMY|DÜŞMAN
Ранения сохраняются между боями|Wounds carry over between battles|Yaralar savaşlar arasında kalır
Реклама: +1 расходник|Ad: +1 consumable|Reklam: +1 tüketilebilir eşya
Реклама: +50 денариев|Ad: +50 denarii|Reklam: +50 denarius
Реклама: восстановить отряд|Ad: heal squad|Reklam: ekibi iyileştir
Реклама: удвоить награду за бой|Ad: double battle reward|Reklam: savaş ödülünü ikiye katla
Ретиарий|Retiarius|Retiarius
Ретиариям +10% урона и +0,1 дальности.|+10% damage and +0.1 range to retiarii.|Retiariuslara +%10 hasar ve +0,1 menzil.
Рывок|Dash|Atılma
Сбросить весь прогресс?|Reset all progress?|Tüm ilerleme sıfırlansın mı?
Сбросить всё|Reset everything|Her şeyi sıfırla
Секутор|Secutor|Secutor
Сеть|Net|Ağ
Скорость передвижения +35% на 5 секунд в радиусе 1,8, включая врагов. Не ускоряет атаки. Один заряд.|+35% movement speed for 5 seconds within 1.8 units, including enemies. Does not speed up attacks. One charge.|1,8 birim içinde 5 saniyeliğine +%35 hareket hızı. Düşmanlar da etkilenir. Saldırıları hızlandırmaz. Tek yük.
Скутум|Scutum|Scutum
Строй: без строя|Formation: none|Diziliş: yok
Сытная похлёбка|Hearty stew|Doyurucu yahni
ТРЕНИРОВОЧНЫЙ ДВОР|TRAINING YARD|EĞİTİM AVLUSU
Убийства|Kills|Öldürülen
Удалить забег|Delete run|Koşuyu sil
Удалить забег — сохранить улучшения.\nПолный сброс — начать с чистого листа.|Delete run: keep your upgrades.\nFull reset: start from scratch.|Koşuyu sil: geliştirmeler korunur.\nTam sıfırlama: her şeye baştan başla.
Удар щитом|Shield bash|Kalkan darbesi
Улучшить|Upgrade|Geliştir
Урон · Броня|Damage · Armor|Hasar · Zırh
Ускоряет передвижение на 35% на 2 секунды.|Increases movement speed by 35% for 2 seconds.|Hareket hızını 2 saniyeliğine %35 artırır.
Учебная схватка. Доступна только в раундах 1–3.|Training bout. Available only in rounds 1–3.|Eğitim dövüşü. Yalnızca 1–3. turlarda kullanılabilir.
Фаланга|Phalanx|Falanks
Флакон мази|Ointment flask|Merhem şişesi
Формация|Formation|Diziliş
Фракиец|Thraex|Thraex
Черепаха|Testudo|Kaplumbağa
Школа сетей|Net school|Ağ okulu
Щиты арены|Shields of the arena|Arena kalkanları
{0}: выберите круг на арене. Действует на обе стороны!|{0}: choose an area in the arena. Affects both sides!|{0}: arenada bir alan seçin. Her iki tarafı da etkiler!
Купить · {0} ден.\nВ запасе: {1}|Buy · {0} den.\nIn stock: {1}|Satın al · {0} den.\nStok: {1}
Использовать · {0}|Use · {0}|Kullan · {0}
Постройки|Buildings|Binalar
Арсенал|Armory|Cephanelik
Открывайте бойцов для следующих забегов|Unlock fighters for future runs|Gelecek koşular için savaşçıların kilidini açın
Улучшения школы действуют во всех забегах|School upgrades apply to every run|Okul geliştirmeleri tüm koşularda geçerlidir
Покупайте и надевайте снаряжение для своих бойцов|Buy and equip gear for your fighters|Savaşçılarınız için ekipman satın alın ve kuşanın
Одна покупка — один заряд. Все эффекты в кругу действуют на обе стороны|One purchase, one charge. All effects in the circle affect both sides|Her satın alım bir yük verir. Çemberdeki tüm etkiler her iki tarafa da uygulanır
ОТРЯД В ЗАБЕГЕ  /  {0}|RUN SQUAD  /  {0}|KOŞU EKİBİ  /  {0}
открыт|unlocked|açık
с раунда {0}|from round {0}|{0}. turdan itibaren
{0} ден.|{0} den.|{0} den.
Открыт|Unlocked|Açık
надет|equipped|kuşanıldı
в сундуке|in storage|depoda
Надеть|Equip|Kuşan
Продолжить забег|Continue run|Koşuya devam et
Денарии: {0}|Denarii: {0}|Denarius: {0}
максимум|max|azami
Не удалось открыть арену. Попробуйте ещё раз.|Could not open the arena. Please try again.|Arena açılamadı. Lütfen tekrar deneyin.
Сохранённый забег · раунд {0}|Saved run · round {0}|Kayıtlı koşu · tur {0}
Реклама: +1 |Ad: +1 |Reklam: +1 
расходник|consumable|tüketilebilir eşya
Реклама: восстановить весь отряд|Ad: heal the whole squad|Reklam: tüm ekibi iyileştir
Бесплатный отдых: {0}:{1:00}|Free rest: {0}:{1:00}|Ücretsiz dinlenme: {0}:{1:00}
Восстановить отряд бесплатно|Heal squad for free|Ekibi ücretsiz iyileştir
Нет активного отряда|No active squad|Aktif ekip yok
Все бойцы здоровы|All fighters are healthy|Tüm savaşçılar sağlıklı
Ранения сохраняются между боями.\nБез сознания: {0}. Отдых — 2 минуты.|Wounds carry over between battles.\nUnconscious: {0}. Rest takes 2 minutes.|Yaralar savaşlar arasında kalır.\nBaygın: {0}. Dinlenme süresi: 2 dakika.
Реклама: ещё +{0} денариев за бой|Ad: +{0} extra denarii for this battle|Reklam: bu savaş için +{0} ek denarius
ПОБЕДА!|VICTORY!|ZAFER!
Раунд {0}|Round {0}|Tur {0}
Заработано {0}|Earned {0}|Kazanılan: {0}
{0} золота|{0} gold|{0} altın
Денарии: {0}   •   Лучший раунд: {1}\n|Denarii: {0}   •   Best round: {1}\n|Denarius: {0}   •   En iyi tur: {1}\n
Покупка навсегда. Денарии пополняются при возврате на базу.|Permanent purchase. Denarii are credited when you return to base.|Kalıcı satın alım. Üsse döndüğünüzde denarius kazanırsınız.
Всегда доступно · бесплатно|Always available · free|Her zaman açık · ücretsiz
Куплено навсегда|Permanently owned|Kalıcı olarak alındı
Достигните раунда {0} · {1} ден.|Reach round {0} · {1} den.|{0}. tura ulaşın · {1} den.
Доступно для покупки · {0} ден.|Available to buy · {0} den.|Satın alınabilir · {0} den.
Нужно {0} ден. · не хватает {1}|Costs {0} den. · need {1} more|Bedeli: {0} den. · {1} daha gerekli
Закрыто|Locked|Kilitli
Купить · {0} ден.|Buy · {0} den.|Satın al · {0} den.
никого|none|yok
Без строя|No formation|Diziliş yok
Отряд идёт врассыпную и сходится с врагом сразу, без бонусов и без ожидания.|The squad advances freely and engages the enemy immediately, with no bonuses or delay.|Ekip serbestçe ilerler ve bonus veya bekleme olmadan düşmanla hemen çatışır.
Золото: {0}|Gold: {0}|Altın: {0}
Отряд: {0}|Squad: {0}|Ekip: {0}
Строй: {0}|Formation: {0}|Diziliş: {0}
Поражение в раунде {0}|Defeated in round {0}|{0}. turda yenildiniz
{0} бойцов|Fighters: {0}|Savaşçı: {0}
HP {0}|HP {0}|Can {0}
Урон {0}|Damage {0}|Hasar {0}
Скорость {0:0.0}|Speed {0:0.0}|Hız {0:0.0}
С кем выходим на песок?|Who will enter the arena?|Arenaya kim çıkacak?
Урон {0}  ·  Броня {1}|Damage {0}  ·  Armor {1}|Hasar {0}  ·  Zırh {1}
Подключение рекламы…|Connecting to ads…|Reklam servisine bağlanılıyor…
Открывается реклама…|Opening ad…|Reklam açılıyor…
Реклама недоступна. Игра доступна без неё.|Ads are unavailable. You can keep playing.|Reklamlar kullanılamıyor. Oynamaya devam edebilirsiniz.
Награда получена|Reward received|Ödül alındı
Реклама закрыта или недоступна|Ad closed or unavailable|Reklam kapatıldı veya kullanılamıyor
Скорость боя ×{0}|Battle speed ×{0}|Savaş hızı ×{0}'''

entries = {}
for line in data.splitlines():
    key, en, tr = (s.replace('\\n','\n') for s in line.split('|'))
    entries[key] = dict(key=key, ru=key, en=en, tr=tr)

aliases = {'Accept\n':'Получить','Button':'Выбрать','Clear':'Закрыть','Descr':'Описание','Fight\n':'В бой','Lupanarium\n':'В лупанарий','Name':'Название','Name\n':'Название','New Text':'Описание','Restart\n':'Начать новый забег','Stats':'Урон · Броня'}
for key, target in aliases.items():
    entries[key] = dict(entries[target], key=key)
for key in ['LUPANARIUM','×','0','0/0','10','10|10','100\n','250','50']:
    entries[key] = dict(key=key,ru=key,en=key,tr=key)
key='Fast, agile, cunning. Fights from a distance using a net and a trident.'
entries[key]=dict(key=key,ru='Быстрый, ловкий, хитрый. Сражается на расстоянии с сетью и трезубцем.',en=key,tr='Hızlı, çevik ve kurnaz. Ağ ve üç dişli mızrakla uzaktan savaşır.')
authored=json.loads(Path('Tools/Localization/authored.json').read_text(encoding='utf-8-sig'))
code=json.loads(Path('Tools/Localization/code-strings.json').read_text(encoding='utf-8-sig'))
for key in authored:
    if key.startswith('Slow but well-armoured'):
        entries[key]=dict(key=key,ru='Медленный, но хорошо защищённый — классический «танк».',en='Slow but well-armored — a classic tank.',tr='Yavaş ama iyi zırhlı, tam bir tank.')
missing=set(authored)|set(code)
missing-=entries.keys()
if missing: raise ValueError('Missing translations: '+repr(missing))
for e in entries.values():
    e['context']='; '.join(authored.get(e['key'],[])+code.get(e['key'],[]))
    fields=lambda s: sorted(re.findall(r'\{\d+(?::[^}]+)?\}',s))
    assert fields(e['key'])==fields(e['en'])==fields(e['tr']),e['key']
Path('Tools/Localization/translations.json').write_text(json.dumps({'entries':list(entries.values())},ensure_ascii=False,indent=2),encoding='utf-8')
print('Translations:',len(entries),'x 3 locales; no missing keys or changed placeholders')
