"""Rebuild project-owned, grid-aligned SVG icons and quiet WAV cues. No third-party input."""
from __future__ import annotations

import json
import hashlib
import math
import re
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUTLINE = '#4A3728'
IVORY = '#FFF0CF'
GOLD = '#C69B55'
JADE = '#7D9875'
PALETTE = {'jade': JADE, 'water': '#739899', 'bone': '#D1BA92',
           'violet': '#91829E', 'iron': '#819092', 'red': '#AE7060', 'gold': GOLD}


def rect(x, y, w, h, color, stroke=False):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{color}"' + (f' stroke="{OUTLINE}" stroke-width="2"' if stroke else '') + '/>'


def poly(points, color, stroke=True):
    return f'<polygon points="{points}" fill="{color}"' + (f' stroke="{OUTLINE}" stroke-width="2" stroke-linejoin="miter"' if stroke else '') + '/>'


def line(points, color=OUTLINE, width=2):
    return f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{width}"/>'


def shape(kind, color=JADE, variant=0):
    light = IVORY
    if kind == 'herb':
        return line('15,27 15,8') + poly('15,19 6,18 4,10 12,11 15,15',color) + poly('15,14 19,6 27,4 25,13 19,17',color) + rect(7,12,3,2,light)
    if kind == 'flower':
        return line('16,28 16,15') + poly('16,3 20,8 26,8 26,14 21,17 20,23 14,23 11,18 5,16 5,10 12,8',color) + rect(13,10,6,6,GOLD,True)
    if kind == 'fruit':
        return poly('8,10 14,8 23,10 27,16 25,25 19,28 10,26 5,21 5,15',color) + poly('15,9 17,3 25,4 22,8',JADE) + rect(9,13,4,5,light)
    if kind == 'seed':
        return poly('8,8 12,4 23,4 26,9 24,14 27,27 6,27 9,14',light) + rect(8,11,17,3,GOLD) + poly('15,22 12,18 15,15 20,16 21,20 18,23',color)
    if kind == 'ore':
        return poly('3,24 6,13 13,6 21,5 28,14 26,26 14,28',color) + poly('7,16 13,8 17,8 14,18',light,False) + line('14,18 25,13 26,25')
    if kind in ('gem','pearl','shard'):
        points = '16,3 25,8 29,17 24,25 15,29 6,24 3,15 8,6' if kind=='pearl' else '12,3 24,6 28,16 21,29 7,24 3,15'
        return poly(points,color) + poly('8,8 15,5 18,12 12,17 6,16',light,False) + line('18,12 26,16 21,26',GOLD)
    if kind == 'fish':
        return poly('4,16 10,9 21,10 25,14 29,8 29,24 25,19 18,23 10,23',color) + poly('12,10 16,5 21,10',GOLD) + rect(9,14,3,3,OUTLINE) + line('17,12 20,16 17,21',light)
    if kind == 'bottle':
        return rect(12,3,8,4,GOLD,True) + poly('12,7 20,7 20,12 25,16 25,27 7,27 7,16 12,12',light) + rect(9,18,14,7,color) + rect(11,12,3,5,'#FFFFFF') + rect(14,20,4,3,GOLD)
    if kind == 'powder':
        return poly('7,5 25,5 21,12 26,18 26,27 6,27 6,18 11,12',light) + rect(8,9,16,3,GOLD) + poly('9,24 13,18 19,19 23,24',color,False)
    if kind in ('token','talisman','page','ink'):
        if kind == 'ink':
            return poly('7,7 25,7 27,23 21,27 6,25',OUTLINE) + rect(10,10,12,10,color) + line('11,21 20,17',GOLD) + rect(23,2,3,13,GOLD)
        return poly('7,3 23,3 26,7 26,29 6,29 6,7',light) + rect(8,5,15,3,color) + line('11,12 19,12 14,17 21,20 12,24',color,3) + (rect(20,23,5,5,GOLD) if kind=='token' else '')
    if kind == 'bone':
        return poly('4,6 8,3 12,7 11,11 22,20 26,18 29,23 26,28 22,26 20,22 8,13 4,13 2,9',color) + line('10,10 21,21',light)
    if kind == 'bait':
        return line('5,11 9,5 15,8 14,16 20,19 23,14 28,18',color,6) + rect(8,7,2,2,OUTLINE) + rect(11,24,9,3,GOLD)
    if kind in ('soup','slices','food'):
        body=poly('4,16 28,16 26,25 22,29 10,29 6,25',color) + rect(4,14,24,4,light,True)
        if kind=='slices':
            return body + poly('7,12 10,8 16,9 14,13', '#AE7060',False) + poly('17,12 20,8 25,9 24,13', '#AE7060',False)
        return body + line('10,12 8,8 11,3',GOLD) + line('21,12 19,8 22,3',GOLD) + rect(11,18,4,3,light) + (rect(19,19,4,3,GOLD) if variant%2 else '')
    if kind in ('box','fragment'):
        if kind=='fragment':
            return poly('5,9 15,6 18,13 25,10 28,23 18,27 13,22 4,25',color) + line('15,7 13,17 20,20',light)
        return rect(4,11,24,17,color,True) + poly('4,11 9,5 23,5 28,11',light) + rect(13,13,6,7,GOLD,True)
    if kind in ('plate','flag'):
        if kind=='flag':
            return rect(6,3,3,26,GOLD) + poly('9,4 27,4 24,11 27,18 9,18',color) + line('14,8 21,8 18,14',light)
        return poly('10,3 22,3 29,10 29,22 22,29 10,29 3,22 3,10',color) + poly('11,8 21,8 25,12 25,21 20,25 11,25 7,20 7,12',light) + line('10,16 16,9 22,16 16,23 10,16',GOLD)
    if kind in ('sword','dagger','spear','staff'):
        if kind=='staff':
            return line('7,28 22,8',OUTLINE,5) + line('7,27 22,8',GOLD,2) + poly('17,4 22,2 28,7 25,13 19,12',color) + rect(20,5,4,4,light)
        if kind=='spear':
            return line('5,29 23,9',OUTLINE,5) + line('5,28 22,9',GOLD,2) + poly('20,6 29,2 27,12 21,15',color)
        tip=22 if kind=='dagger' else 29
        return poly(f'7,20 10,17 {tip-6},5 {tip},2 {tip-1},9 14,23',color) + line(f'12,17 {tip-2},5',light) + line('6,16 17,27',GOLD,4) + line('10,23 4,29',OUTLINE,5) + rect(3,26,4,4,GOLD)
    if kind == 'robe':
        return poly('11,5 21,5 29,13 25,18 22,14 26,29 6,29 10,14 7,18 3,13',color) + poly('11,5 16,12 21,5 18,17 14,17',light,False) + rect(10,18,13,3,GOLD) + line('15,21 13,28',light)
    if kind in ('armor','mail','scale'):
        body=poly('9,4 14,8 18,8 23,4 29,12 24,18 23,28 9,28 8,18 3,12',color)
        if kind=='armor': return body + poly('12,12 20,12 22,21 16,26 10,21',light) + rect(14,14,4,5,GOLD)
        return body + ''.join(line(f'10,{y} 14,{y+3} 18,{y} 22,{y+3}',light) for y in (12,18,24))
    if kind in ('ring','pendant','wreath','chain'):
        if kind=='pendant': return line('7,4 16,13 25,4',GOLD,3)+poly('13,12 19,12 23,21 16,29 9,21',color)+rect(14,17,3,5,light)
        body=poly('9,5 23,5 28,10 28,23 23,28 9,28 4,23 4,10',GOLD)+poly('10,11 22,11 23,22 10,22', '#FFF0CF')
        if kind=='wreath': return body + poly('3,9 8,3 12,7 7,14',color) + poly('21,7 26,3 30,9 25,14',color) + rect(5,21,5,5,color) + rect(22,21,5,5,color)
        if kind=='chain': return body + ''.join(rect(x,y,5,5,color,True) for x,y in ((4,10),(4,20),(23,10),(23,20),(13,25)))
        return body+poly('12,3 20,3 23,7 20,12 12,12 9,7',color)+rect(13,5,3,3,light)
    if kind=='book':
        return poly('3,5 13,5 16,8 19,5 29,5 29,26 19,26 16,29 13,26 3,26',light)+line('16,9 16,26',OUTLINE)+line('6,10 12,10',color)+line('20,10 26,10',color)+line('6,15 12,15',GOLD)+line('20,15 26,15',GOLD)
    if kind=='lock': return line('9,15 9,8 12,4 20,4 23,8 23,15',GOLD,4)+rect(5,14,22,15,color,True)+rect(14,19,4,5,light)
    if kind=='coin': return poly('10,3 22,3 29,10 29,22 22,29 10,29 3,22 3,10',GOLD)+rect(12,12,8,8,light,True)
    if kind=='backpack': return rect(7,7,18,23,color,True)+line('11,7 11,3 21,3 21,7',GOLD,3)+rect(9,18,14,9,light,True)+rect(14,19,4,3,GOLD)
    if kind=='cave': return poly('3,28 4,16 11,5 20,3 28,15 29,28',color)+poly('11,28 11,18 15,12 20,15 23,28',OUTLINE)+rect(7,12,3,3,GOLD)
    if kind=='hammer': return line('7,29 20,12',OUTLINE,5)+line('8,27 20,12',GOLD,2)+poly('13,6 19,2 30,12 24,18',color)+line('20,5 26,11',light)
    if kind=='arm': return poly('4,25 7,16 13,14 15,8 13,5 19,3 24,7 22,15 29,20 28,28 10,29',color)+line('10,24 18,21 25,23',GOLD)
    if kind=='pick': return line('7,29 21,9',OUTLINE,5)+line('7,27 21,9',GOLD,2)+poly('8,8 17,3 26,5 30,13 22,9 15,10',color)
    if kind=='settings': return poly('12,2 20,2 21,7 26,7 30,13 26,17 28,23 22,28 18,25 13,30 7,26 7,21 2,18 4,11 9,10',color)+rect(11,11,10,10,light,True)
    if kind=='check': return line('4,16 12,24 28,6',OUTLINE,6)+line('4,16 12,24 28,6',color,3)
    if kind=='cauldron': return poly('7,12 25,12 29,17 26,26 22,29 10,29 6,26 3,17',color)+line('4,15 1,10 6,8',GOLD,3)+line('28,15 31,10 26,8',GOLD,3)+poly('12,10 11,5 16,2 20,5 18,10',GOLD)+rect(11,18,10,4,light)
    return poly('10,5 22,5 28,13 26,25 8,27 3,17',color)+rect(13,11,6,6,light)+rect(15,21,3,3,GOLD)


def svg(kind, color, variant=0, app=False, motif=''):
    inner=shape(kind,color,variant)
    if motif == 'bloom':
        inner += rect(14,20,4,4,'#AE7060') + rect(15,21,2,2,GOLD)
    elif motif == 'moss':
        inner += line('10,23 12,25 14,22', '#55704F', 2)
    elif motif == 'seal':
        inner += rect(17,18,5,6,IVORY,True) + rect(19,20,1,2,GOLD)
    if app:
        inner=poly('5,1 27,1 31,5 31,27 27,31 5,31 1,27 1,5',IVORY)+inner
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{128 if app else 48}" height="{128 if app else 48}" viewBox="0 0 32 32" shape-rendering="crispEdges">{inner}</svg>\n'


ITEMS = {
 'spirit_herb':('herb','jade'), 'spirit_flower':('flower','red'), 'spirit_fruit':('fruit','gold'),
 'seed_spirit_herb':('seed','jade'), 'seed_spirit_flower':('seed','red'), 'seed_spirit_fruit':('seed','gold'),
 'cold_iron_ore':('ore','iron'), 'spirit_jade':('gem','jade'), 'mithril':('ore','violet'),
 'spirit_fish':('fish','jade'), 'spirit_pearl':('pearl','water'), 'dragon_saliva':('bottle','gold'),
 'ripening_elixir':('bottle','jade'), 'mining_refresh_token':('token','iron'), 'fishing_bait':('bait','red'),
 'page_fragment':('page','bone'), 'lingqi_shard':('shard','jade'), 'broken_talisman':('fragment','gold'),
 'spirit_ink':('ink','violet'), 'beast_bone':('bone','bone'),
 'talisman_fire_charm':('talisman','red'), 'talisman_shield_charm':('talisman','jade'),
 'food_clear_fish_soup':('soup','jade'), 'food_spirit_fish_slices':('slices','water'),
 'food_focus_fish_stew':('soup','violet'), 'food_iron_fish_meal':('food','iron'),
 'cold_scale_fish':('fish','iron'), 'deep_abyss_fish':('fish','violet'), 'armored_fish':('fish','bone'),
 'spirit_spring_water':('bottle','water'), 'water_spirit_amber':('gem','water'),
 'sunk_jade_box':('box','jade'), 'sunk_equipment_fragment':('fragment','iron'),
 'food_spirit_porridge':('soup','bone'), 'food_fruit_jelly':('food','gold'), 'food_dragon_soup':('soup','gold'),
 'formation_spirit_plate':('plate','jade'), 'formation_guard_flag':('flag','iron'),
 'formation_harvest_array':('plate','gold'), 'formation_craft_array':('plate','violet'),
 'potion_huiqi_dan':('bottle','red'), 'potion_juling_san':('powder','jade'), 'talisman_burst_charm':('talisman','gold'),
}
UI = {
 'book':('book','jade'), 'lock':('lock','jade'), 'coin':('coin','gold'), 'backpack':('backpack','jade'),
 'settings':('settings','iron'), 'check':('check','jade'), 'item_unknown':('unknown','bone'),
 'weapon':('sword','iron'), 'armor':('armor','bone'), 'accessory':('pendant','jade'),
 'dungeon':('cave','bone'), 'cultivation':('gem','jade'), 'alchemy':('cauldron','jade'),
 'smithing':('hammer','iron'), 'body_cultivation':('arm','bone'), 'garden':('herb','jade'),
 'mining':('pick','iron'), 'fishing':('fish','water'), 'cooking':('soup','jade'),
 'talisman':('talisman','gold'), 'formation':('plate','jade'),
}


def equipment_style(t):
    icon=t['icon_id']
    kind=next((k for k in ('dagger','spear','staff','robe','mail','plate','scale','pendant','wreath','chain','bead','band','ring','blade','sword') if k in icon),t['slot'])
    kind={'plate':'armor','bead':'pearl','band':'ring','blade':'sword','weapon':'sword','accessory':'pendant'}.get(kind,kind)
    series=t['series_id']
    accent=next((c for tag,c in [('venom','violet'),('shadow','violet'),('forge','iron'),('garden','jade'),('sunken','water'),('runic','gold')] if tag in series),'jade')
    if 'bone' in t['equipment_template_id']: accent='bone'
    if 'coral' in t['equipment_template_id']: accent='red'
    return kind,accent


def write_icon(relative,kind,accent,variant=0,app=False,motif=''):
    target=ROOT/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(svg(kind,PALETTE[accent],variant,app,motif),encoding='utf-8',newline='\n')


def item_family(item):
    if item.startswith('seed_'): return 'seeds'
    if item.startswith('food_'): return 'food'
    if item.startswith('formation_'): return 'formations'
    if item.startswith(('potion_', 'talisman_')) or item in ('ripening_elixir','mining_refresh_token','fishing_bait'): return 'consumables'
    return 'materials'


def main():
    entries=[]
    def add(key,relative,kind,accent,**extra):
        write_icon(relative,kind,accent,extra.get('variant',0),motif=extra.get('motif',''))
        entries.append({'key':key,'path':'res://'+relative,'category':key.split(':')[0],
                        'source':{'method':'project_vector','recipe':kind,'accent':accent},**extra})
    for i,(item,(kind,accent)) in enumerate(ITEMS.items()):
        family=item_family(item)
        add('item:'+item,f'assets/items/{family}/{item}_v1.svg',kind,accent,variant=i,family=family)
    templates=json.loads((ROOT/'docs/design/12_equipment_sample_qi_refining.json').read_text(encoding='utf-8-sig'))['equipment_templates']
    for i,t in enumerate(templates):
        kind,accent=equipment_style(t)
        motif=next((name for tag,name in [('garden','bloom'),('outer','moss'),('runic','seal')] if tag in t['series_id']),'')
        if 'jade_pendant' in t['equipment_template_id']: accent='jade'
        add('equipment:'+t['equipment_template_id'],f"assets/items/equipment/{t['icon_id']}_v1.svg",kind,accent,
            template_id=t['equipment_template_id'],icon_id=t['icon_id'],family=t['series_id'],variant=i,motif=motif,display_name=t['display_name'])
    for name,(kind,accent) in UI.items():
        add('ui:'+name,f'assets/ui/icons/{name}_v1.svg',kind,accent)
    write_icon('assets/ui/icons/application_v1.svg','cauldron','jade',app=True)
    entries.append({'key':'ui:application','path':'res://assets/ui/icons/application_v1.svg','category':'ui','source':{'method':'project_vector','recipe':'cauldron','accent':'jade'}})
    prompts=json.loads((ROOT/'docs/art/generation_prompts.json').read_text(encoding='utf-8'))
    entries.append({'key':'player:cultivator_idle','path':'res://assets/characters/player/cultivator_idle_v1.png','category':'player','source':{'method':'builtin_imagegen','prompt_id':'hero'}})
    for p in prompts['monsters']:
        entries.append({'key':'monster:'+p['id'],'path':f"res://assets/characters/monsters/{p['id']}_idle_v1.png",'category':'monster','source':{'method':'builtin_imagegen','prompt_id':p['id'],'style_reference':'player:cultivator_idle'}})
    audio={'ui_soft':([660],0.09),'craft_ready':([440,660],0.26),'battle_win':([330,440,660],0.42),
           'realm_up':([330,440,550,660],0.65),'resource_hint':([330,294],0.3)}
    for name,(notes,duration) in audio.items():
        relative=f'assets/audio/sfx/{name}_v1.wav'
        target=ROOT/relative;target.parent.mkdir(parents=True,exist_ok=True)
        rate=22050
        frames=[]
        for i in range(round(duration*rate)):
            t=i/rate;slot=min(int(t/duration*len(notes)),len(notes)-1)
            local=t-slot*duration/len(notes);span=duration/len(notes)
            envelope=min(1,local/0.008)*max(0,1-local/span)**2
            value=0.09*envelope*math.sin(2*math.pi*notes[slot]*local)
            frames.append(struct.pack('<h',round(value*32767)))
        with wave.open(str(target),'wb') as f:
            f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(b''.join(frames))
        entries.append({'key':'sfx:'+name,'path':'res://'+relative,'category':'sfx','source':{'method':'project_synthesis','recipe':{'notes_hz':notes,'seconds':duration,'peak_amplitude':0.09}},'usage':'optional_event_cue'})
    entries.append({'key':'font:ui_sans_sc','path':'res://assets/fonts/ui_sans_sc_regular.tres','category':'font',
                    'source':{'method':'external_original','url':'https://github.com/google/fonts/tree/main/ofl/notosanssc',
                              'download_url':'https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf',
                              'license':'SIL Open Font License 1.1','license_path':'assets/fonts/OFL.txt',
                              'font_file':'assets/fonts/NotoSansSC-Variable.ttf',
                              'font_file_sha256':hashlib.sha256((ROOT/'assets/fonts/NotoSansSC-Variable.ttf').read_bytes()).hexdigest(),
                              'weight':400}})
    manifest={'schema_version':1,'style_family':'xianxia_parchment_pixel_v1','palette':{'outline':OUTLINE,'ivory':IVORY,**PALETTE},'entries':entries}
    for entry in entries:
        path=ROOT/entry['path'].removeprefix('res://')
        if path.exists():
            entry['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    (ROOT/'assets/asset_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    build_gallery(manifest)
    print(f'Built {len(ITEMS)} item icons, {len(templates)} equipment icons, {len(UI)+1} UI icons and {len(audio)} quiet cues; registered 12 character sprites.')


def build_gallery(manifest):
    """Embed the index so this review gallery works from file:// without a server."""
    data=json.dumps(manifest,ensure_ascii=False).replace('</','<\\/')
    html='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>修仙素材图鉴</title><style>
@font-face{font-family:GameSans;src:url('../../assets/fonts/NotoSansSC-Variable.ttf')}*{box-sizing:border-box}body{margin:0;background:#f3e5ca;color:#4a3728;font-family:GameSans,sans-serif}header,main{max-width:1400px;margin:auto;padding:24px}h1{margin:0 0 12px}p{line-height:1.6}.toolbar{display:flex;flex-wrap:wrap;gap:8px;position:sticky;top:0;padding:16px 24px;background:#ead8b8;z-index:2}button,input{font:inherit;border:1px solid #8b7050;border-radius:5px;padding:7px 12px;background:#fff0cf;color:#4a3728}button[aria-pressed=true]{background:#5b4838;color:#fff6e2}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(185px,1fr));gap:12px}.card{background:#fff5df;border:1px solid #c7ad82;border-radius:6px;padding:14px;min-width:0}.card a{color:inherit;text-decoration:none}.preview{height:144px;display:flex;align-items:center;justify-content:center;background:repeating-conic-gradient(#eadfca 0 25%,#f7eedf 0 50%) 0/16px 16px;border-radius:3px}.preview img{max-width:128px;max-height:128px;image-rendering:pixelated}.card[data-category=item] img,.card[data-category=equipment] img,.card[data-category=ui] img{width:48px;height:48px}.key{font-size:11px;overflow-wrap:anywhere;margin:8px 0;color:#6f5a44}.method{font-size:12px}audio{width:100%}[hidden]{display:none!important}small{color:#6f5a44}.card .size{display:flex;gap:12px;align-items:center;margin-top:8px}.card .size img{width:24px;height:24px;image-rendering:pixelated}footer{padding:24px}
</style><header><h1>修仙 · 素材图鉴</h1><p>暖棕轮廓、米白、玉绿与铜金。点击素材可查看原文件；角色下方提供 24px 缩小预览。音效由你点击试听。</p><small>素材规范与扩展提示词：<a href="../design/20_asset_direction.md">设计文档</a> · <a href="generation_prompts.json">实际生成提示词</a> · <a href="../../assets/asset_manifest.json">素材索引</a></small></header><nav class="toolbar" aria-label="素材筛选"><button data-filter="all" aria-pressed="true">全部</button><button data-filter="player">主角</button><button data-filter="monster">怪物</button><button data-filter="item">物品</button><button data-filter="equipment">装备</button><button data-filter="ui">界面</button><button data-filter="sfx">音效</button><button data-filter="font">字体</button><input id="query" type="search" placeholder="搜索素材 ID 或名称" aria-label="搜索素材"><span id="count" aria-live="polite"></span></nav><main><div id="grid" class="grid"></div></main><footer>素材来源见索引。第三方字体许可证见 assets/fonts/OFL.txt。任务进度只维护于 docs/design/10_todo.md。</footer>
<script>const manifest=__DATA__;const grid=document.getElementById('grid');let filter='all';
const labels={player:'主角',monster:'怪物',item:'物品',equipment:'装备',ui:'界面',sfx:'音效',font:'字体'};
const methods={builtin_imagegen:'imagegen 生成',project_vector:'项目自制 SVG',project_synthesis:'项目合成 WAV',external_original:'官方原始文件'};
for(const e of manifest.entries){const card=document.createElement('article');card.className='card';card.dataset.category=e.category;card.dataset.search=(e.key+' '+(e.display_name||'')).toLowerCase();const url='../../'+e.path.replace('res://','');const area=document.createElement('div');area.className='preview';if(e.category==='sfx'){const audio=document.createElement('audio');audio.controls=true;audio.preload='none';audio.src=url;area.append(audio)}else if(e.category==='font'){area.textContent='清心修行 · 永 0123456789'}else{const a=document.createElement('a');a.href=url;a.target='_blank';a.rel='noopener';const img=document.createElement('img');img.src=url;img.alt=e.display_name||e.key;img.loading='lazy';a.append(img);area.append(a)}card.append(area);if(['monster','player'].includes(e.category)){const size=document.createElement('div');size.className='size';const img=document.createElement('img');img.src=url;img.alt='24像素缩略图';const caption=document.createElement('small');caption.textContent='24px';size.append(img,caption);card.append(size)}const key=document.createElement('p');key.className='key';key.textContent=(e.display_name?e.display_name+' · ':'')+e.key;card.append(key);const method=document.createElement('small');method.className='method';method.textContent=labels[e.category]+' · '+methods[e.source.method];card.append(method);grid.append(card)}
function update(){let count=0;for(const card of grid.children){card.hidden=!(filter==='all'||card.dataset.category===filter)||!card.dataset.search.includes(document.getElementById('query').value.toLowerCase());if(!card.hidden)count++}document.getElementById('count').textContent=count+' 项'}document.querySelectorAll('[data-filter]').forEach(btn=>btn.addEventListener('click',()=>{filter=btn.dataset.filter;document.querySelectorAll('[data-filter]').forEach(b=>b.setAttribute('aria-pressed',String(b===btn)));update()}));document.getElementById('query').addEventListener('input',update);update();</script></html>'''
    (ROOT/'docs/art/asset_gallery.html').write_text(html.replace('__DATA__',data),encoding='utf-8',newline='\n')


if __name__=='__main__':
    main()
