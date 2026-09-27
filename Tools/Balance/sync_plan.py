import json, re
from pathlib import Path
root = Path(__file__).parent
plan = json.loads((root / 'balance-v1.json').read_text(encoding='utf-8'))
health = dict(Murmillo=600, Retiatius=475, Secutor=650, Thraex=450, Hoplomach=675, Wolf=375, Lion=875)
script = (root / 'ApplyBalance.cs').read_text(encoding='utf-8')
for u in plan['units']:
    u['props']['_maxHealth'] = health[u['name']]
    pattern = r'(Gladiators/' + u['name'] + r'\.asset",so=>\{Set\(so.FindProperty\("_maxHealth"\),)\d+'
    script = re.sub(pattern, lambda m: m[1] + str(health[u['name']]), script)
for b in plan['blessings']:
    if b['name'] == 'fervor': continue
    old, new = ('120','150') if b['name']=='ointment' else ('80','100')
    b['power'] = int(new)
    b['desc'] = b['desc'].replace(old, new)
    script = script.replace('"_power"),'+old+');', '"_power"),'+new+');')
    script = script.replace(old+' здоровья',new+' здоровья').replace(old+' урона',new+' урона')
(root / 'balance-v1.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
(root / 'ApplyBalance.cs').write_text(script,encoding='utf-8')
