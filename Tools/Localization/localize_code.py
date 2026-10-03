import re,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
entries={}
files=list((root/'Assets/Code/Presentation').rglob('*.cs'))+[root/'Assets/Code/Gameplay/Hud/HealthClassRowView.cs',root/'Assets/Code/Infrastructure/Platform/YandexBridge.cs']
for p in files:
 s=p.read_text(encoding='utf-8-sig'); spans=[]
 for pattern in [r'\[(?:Header|Tooltip)\([\s\S]*?\)\]',r'Debug\.\w+\([\s\S]*?\);',r'throw new [\s\S]*?;',r'private const string [\s\S]*?;',r'private static readonly string\[\] [\s\S]*?;']:
  spans += [(m.start(),m.end()) for m in re.finditer(pattern,s)]
 def replace(m):
  raw=m.group(); start=m.start()
  if any(a<=start<b for a,b in spans) or 'L10n.' in s[max(0,start-12):start]:return raw
  if not re.search('[А-Яа-яЁё]',raw) and not raw.startswith('$"HP '):return raw
  if s[s.rfind('\n',0,start)+1:start].lstrip().startswith('//'):return raw
  val=json.loads(raw[1:] if raw.startswith('$') else raw)
  if raw.startswith('$'):
   n=[0]
   def arg(a):
    i=n[0];n[0]+=1;fmt=a.group(1).split(':',1)
    return '{'+str(i)+(':'+fmt[1] if len(fmt)>1 else '')+'}'
   val=re.sub(r'\{([^{}]+)\}',arg,val)
  entries.setdefault(val,[]).append(str(p.relative_to(root)))
  return ('L10n.F(' if raw.startswith('$') else 'L10n.Text(')+raw+')'
 s=re.sub(r'\$?"(?:\\.|[^"\\])*"',replace,s)
 if p.name=='BasePanelNavigation.cs':
  s=s.replace('_heading.text = Titles[index];','_heading.text = L10n.Text(Titles[index]);').replace('_description.text = Descriptions[index];','_description.text = L10n.Text(Descriptions[index]);')
  for val in re.findall(r'"([^"\n]+)"',s):
   if re.search('[А-Яа-яЁё]',val):entries.setdefault(val,[]).append(str(p.relative_to(root)))
 if p.name=='RunFlowPresenter.cs':
  s=s.replace('private const string NoFormationName =','private static string NoFormationName => L10n.Text(').replace('"Без строя";','"Без строя");')
  s=s.replace('private const string NoFormationDescription =','private static string NoFormationDescription => L10n.Text(').replace('"без бонусов и без ожидания.";','"без бонусов и без ожидания.");')
  entries['Без строя']=[str(p.relative_to(root))]
  entries['Отряд идёт врассыпную и сходится с врагом сразу, без бонусов и без ожидания.']=[str(p.relative_to(root))]
 p.write_text(s,encoding='utf-8')
for p in (root/'Assets/Code').rglob('*.cs'):
 if p.name.endswith('Config.cs') or p.name=='UnitClassHudCatalog.cs':
  s=p.read_text(encoding='utf-8-sig')
  s=s.replace('Description => _description;','Description => L10n.Text(_description);').replace('DisplayName => _displayName;','DisplayName => L10n.Text(_displayName);').replace(': _displayName;',': L10n.Text(_displayName);').replace('_description ?? string.Empty;','L10n.Text(_description ?? string.Empty);')
  p.write_text(s,encoding='utf-8')
(root/'Tools/Localization/code-strings.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),encoding='utf-8')
print('Converted',len(entries),'source templates')
