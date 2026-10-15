"""Project-owned, deliberately plain actor silhouettes. Original v1 PNGs remain available."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
INK, PAPER, JADE, GOLD = '#4A3728', '#FFF0CF', '#7D9875', '#C69B55'
RED, WATER, VIOLET, BONE = '#AE7060', '#739899', '#91829E', '#D1BA92'


def p(points, fill, outline=True):
    stroke = f' stroke="{INK}" stroke-width="1.2" stroke-linejoin="round"' if outline else ''
    return f'<polygon points="{points}" fill="{fill}"{stroke}/>'


def r(x, y, w, h, fill=INK):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}"/>'


def line(points, color=INK, width=1.2):
    return f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round"/>'


def actor(kind):
    if kind == 'cultivator_idle':
        return (p('12,4 14,2 17,3 17,5', INK) + p('11,6 14,4 20,5 21,8 20,13 14,14 11,11', PAPER)
                + p('11,7 12,5 19,5 21,7 17,7 14,10 11,10', INK) + r(19,9,1,1)
                + p('13,14 19,13 23,18 24,26 19,28 10,27 9,20', JADE)
                + p('13,14 17,18 18,25 15,26 14,20', PAPER, False)
                + p('10,16 7,21 10,23 14,19', PAPER) + line('11,22 20,22', GOLD, 2)
                + p('11,27 15,27 14,30 10,30', INK) + p('19,27 22,26 23,30 19,30', INK))
    if kind == 'monster_slime_moss':
        return (p('4,26 5,20 9,15 21,14 26,18 29,26 25,28 8,28', JADE)
                + p('14,15 13,9 8,7 7,10 11,12 14,12 18,7 23,7 21,11 16,12', JADE)
                + r(10,20,2,3) + r(20,20,2,3) + line('14,25 17,25'))
    if 'bat' in kind:
        boss = kind.endswith('lord')
        wings = '3,7 10,12 13,13 12,23 8,19 3,22 4,15' if boss else '3,10 11,14 13,18 11,24 8,19 3,21 5,15'
        right = '29,7 22,12 19,13 20,23 24,19 29,22 28,15' if boss else '29,10 21,14 19,18 21,24 24,19 29,21 27,15'
        return (p(wings, VIOLET) + p(right, VIOLET) + p('11,12 11,7 15,11 18,11 22,7 21,17 20,25 16,28 12,24', INK)
                + r(13,15,2,2,PAPER) + r(18,15,2,2,PAPER) + (p('14,21 18,21 16,25', GOLD, False) if boss else line('15,21 17,21', PAPER)))
    if 'spider' in kind:
        boss = kind.endswith('queen')
        legs = ''.join(line(points, INK, 2) for points in ['12,18 5,15 3,21', '12,21 5,22 3,28', '21,18 27,15 29,21', '21,21 27,22 29,28'])
        abdomen = '10,6 16,3 23,5 26,11 24,19 15,21 10,16' if boss else '12,9 20,8 24,13 22,19 15,21 11,17'
        return (legs + p(abdomen, RED) + (p('13,7 17,9 21,7 20,14 15,14', GOLD, False) if boss else r(17,12,3,3,GOLD))
                + p('8,21 10,17 16,17 19,22 16,26 10,26', INK) + r(10,21,2,2,PAPER) + r(15,21,2,2,PAPER))
    if 'serpent' in kind:
        boss = kind.endswith('serpent')
        return (p('11,11 10,17 16,20 24,20 27,24 25,28 10,28 6,24 7,20 11,21 11,24 21,24 20,23 13,23 6,18 6,12', WATER)
                + p('6,13 4,8 7,4 15,4 18,8 15,12 10,14', WATER) + r(7,7,2,2)
                + p('5,11 10,11 10,14 7,14', PAPER, False)
                + (p('7,4 5,1 10,3 13,1 14,4', GOLD) + line('15,20 21,22', GOLD, 2) if boss else p('12,4 15,1 17,5', JADE)))
    if 'sporeling' in kind or 'fungus' in kind:
        boss = 'titan' in kind
        stem = '10,14 23,14 25,23 29,27 23,29 19,25 17,29 11,29 12,24 5,27 4,24 9,20' if boss else '12,14 22,14 23,25 20,28 17,26 13,28 11,25'
        cap = '3,15 5,8 12,3 22,4 28,10 30,16 22,18 12,15' if boss else '5,15 7,9 14,5 23,7 28,14 27,17 15,14'
        return (p(stem, BONE) + p(cap, JADE) + p('10,9 13,6 17,7 17,10', PAPER, False)
                + p('21,10 23,9 25,12 22,12', PAPER, False) + r(13,19,2,2) + r(20,19,2,2)
                + (line('12,24 16,26 22,24', PAPER, 2) if boss else ''))
    boss = 'king' in kind
    return ((p('9,13 23,13 27,28 19,26 16,29 13,26 5,28', VIOLET) if boss else '')
            + (p('10,6 7,2 13,4 16,2 19,4 25,2 22,7', GOLD) if boss else '')
            + p('10,6 15,4 22,6 24,12 21,16 12,16 8,12', PAPER) + r(11,9,3,3) + r(18,9,3,3)
            + p('13,17 20,17 21,23 12,23', BONE) + line('14,19 19,19') + line('14,21 19,21')
            + line('11,17 7,20 5,24', PAPER, 2.5) + line('22,17 26,20 27,24', PAPER, 2.5)
            + line('14,24 12,29 9,29', BONE, 2.5) + line('19,24 21,29 24,29', BONE, 2.5))


def apply_to_manifest(manifest):
    manifest['style_family'] = 'xianxia_parchment_plain_v2'
    for entry in manifest['entries']:
        if entry['category'] not in ('player', 'monster'):
            continue
        kind = entry['key'].split(':', 1)[1]
        category = 'player' if entry['category'] == 'player' else 'monsters'
        relative = f'assets/characters/{category}/{kind}_plain_v2.svg'
        path = ROOT / relative
        path.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 32 32">{actor(kind)}</svg>\n', encoding='utf-8', newline='\n')
        entry['previous_path'] = f'res://assets/characters/{category}/{kind}_v1.png' if category == 'player' else f'res://assets/characters/{category}/{kind}_idle_v1.png'
        entry['path'] = 'res://' + relative
        entry['source'] = {'method': 'project_vector', 'recipe': 'plain_actor_v2', 'grid': 32, 'max_palette_colors': 7}
        entry['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
    monster_paths = {e['key'].split(':', 1)[1]: e['path'] for e in manifest['entries'] if e['category'] == 'monster'}
    config_path = ROOT / 'docs/design/09_level_monster_drop_sample.json'
    config = json.loads(config_path.read_text(encoding='utf-8-sig'))
    for monster in config['monsters']:
        monster['visual']['portrait'] = monster_paths[monster['monster_id']]
    config_path.write_text(json.dumps(config, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')


def main():
    path = ROOT / 'assets/asset_manifest.json'
    manifest = json.loads(path.read_text(encoding='utf-8'))
    apply_to_manifest(manifest)
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    from build_vector_assets import build_gallery
    build_gallery(manifest)


if __name__ == '__main__':
    main()
