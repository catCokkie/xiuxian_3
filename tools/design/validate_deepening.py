"""Check the design blueprint against real IDs, gates and the rule document.

This validates document structure, not combat balance or feature implementation.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESIGN = ROOT / 'docs/design'


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def baseline():
    modes_text = (ROOT / 'scripts/services/PlayerActionState.cs').read_text(encoding='utf-8-sig')
    modes = dict(re.findall(r'const string Mode(\w+) = "([a-z_]+)"', modes_text))
    unlock_text = (ROOT / 'scripts/services/ProgressiveUnlockRules.cs').read_text(encoding='utf-8-sig')
    unlocks = {mode: 1 for mode in modes.values()}
    for realm, body in re.findall(r'if \(normalizedRealmLevel >= (\d+)\)\s*\{(.*?)\}', unlock_text, re.S):
        for mode in re.findall(r'result.Add\(PlayerActionState.Mode(\w+)\)', body):
            unlocks[modes[mode]] = int(realm)
    mastery_text = (ROOT / 'scripts/services/SubsystemMasteryRules.cs').read_text(encoding='utf-8-sig')
    mastery = {}
    for mode, level, cost, realm in re.findall(r'Def\(PlayerActionState.Mode(\w+),\s*(\d+),\s*([\d.]+),\s*(\d+),', mastery_text):
        mastery[modes[mode], int(level)] = (float(cost), int(realm))
    return modes, unlocks, mastery


def new_recipes(rules):
    section = rules.split('### 24.5 ', 1)[1].split('### 24.6 ', 1)[0]
    recipes = {}
    for line in section.splitlines():
        if not line.startswith('| `'):
            continue
        cells = [cell.strip() for cell in line.strip('|').split('|')]
        ids = re.findall(r'`([^`]+)`', cells[0])
        if len(ids) != 2 or len(cells) != 7:
            raise ValueError('Malformed new recipe row: ' + line)
        if ids[0] in recipes:
            raise ValueError('Duplicate recipe in rules: ' + ids[0])
        recipes[ids[0]] = {'output': ids[1], 'materials': re.findall(r'`([^`]+)`', cells[2]), 'gate': cells[6]}
    return recipes


def validate(data):
    errors = []
    modes, unlocks, mastery = baseline()
    rules = (DESIGN / '02_systems.md').read_text(encoding='utf-8-sig')
    route_text = (DESIGN / '22_play_routes.md').read_text(encoding='utf-8-sig')
    recipes = new_recipes(rules)
    known_items = {e['key'].removeprefix('item:') for e in read_json(ROOT / 'assets/asset_manifest.json')['entries'] if e['category'] == 'item'}
    regions = {z['level_id'] for z in read_json(DESIGN / '09_level_monster_drop_sample.json')['levels']}
    equipment = read_json(DESIGN / '12_equipment_sample_qi_refining.json')
    template_ids = {item['equipment_template_id'] for item in equipment['equipment_templates']}
    for recipe in equipment['forge_recipes']:
        if recipe['output_template_id'] not in template_ids:
            errors.append('Forge recipe has no equipment template: ' + recipe['recipe_id'])
        if any(item['item_id'] not in known_items for item in recipe['cost_materials']):
            errors.append('Forge recipe uses a legacy/undefined material: ' + recipe['recipe_id'])
    if data.get('document_kind') != 'design_blueprint' or data.get('runtime_consumed') is not False:
        errors.append('Blueprint must remain explicitly separate from runtime configuration')
    if set(data['systems']) != set(modes.values()) or len(data['systems']) != len(modes):
        errors.append('System list must match all actual activity modes exactly once')
    for collection in ('milestones', 'branches', 'actions'):
        ids = [entry['id'] for entry in data[collection]]
        if len(set(ids)) != len(ids):
            errors.append('Duplicate IDs in ' + collection)
    actions = {a['id']: a for a in data['actions']}
    goals = {g['id']: g for g in data['milestones']}
    used_recipes = set()
    for action in actions.values():
        ident, system = action['id'], action['system']
        if system not in set(modes.values()) | {'inventory', 'shop', 'journal'}:
            errors.append('Unknown action system: ' + ident)
        if action['min_realm'] < unlocks.get(system, 1):
            errors.append('Action precedes its activity unlock: ' + ident)
        for system_id, level in action['required_mastery'].items():
            definition = mastery.get((system_id, level))
            if not definition or definition[1] > action['min_realm'] or unlocks.get(system_id, 1) > action['min_realm']:
                errors.append('Unreachable mastery gate: ' + ident)
        for region in action['required_clears']:
            if region not in regions:
                errors.append('Unknown clear prerequisite: ' + ident)
        recipe_id = action.get('recipe_id')
        if recipe_id:
            used_recipes.add(recipe_id)
            recipe = recipes.get(recipe_id)
            if not recipe:
                errors.append('Recipe missing from authoritative rules: ' + recipe_id)
                continue
            if any(item not in known_items for item in recipe['materials']):
                errors.append('Recipe introduces an undefined material: ' + recipe_id)
            realm = re.search(r'realm≥(\d+)', recipe['gate'])
            labels = {'炼丹':'alchemy','符箓':'talisman','烹饪':'cooking','阵法':'formation'}
            match = re.search(r'(炼丹|符箓|烹饪|阵法)Lv(\d+)', recipe['gate'])
            expected_mastery = {labels[match[1]]: int(match[2])} if match and int(match[2]) > 1 else {}
            region_names = {'一':1, '二':2, '三':3, '四':4, '五':5}
            clear = re.search(r'首通第([一二三四五])区', recipe['gate'])
            expected_clears = [f'lv_qi_{region_names[clear[1]]:03d}'] if clear else []
            if not realm or int(realm[1]) != action['min_realm'] or expected_mastery != action['required_mastery'] or expected_clears != action['required_clears']:
                errors.append('Recipe gates differ between rule document and index: ' + recipe_id)
        elif ident not in rules:
            errors.append('Action is not specified in rules: ' + ident)
    if used_recipes != set(recipes):
        errors.append('Every new recipe must have a referenced operation')
    covered = {a['system'] for a in actions.values()} & set(modes.values())
    if covered != set(modes.values()):
        errors.append('Each existing activity needs a distinct depth operation')
    for branch in data['branches']:
        if branch.get('mandatory_for_main') is not False:
            errors.append('Optional branch cannot become a main route gate: ' + branch['id'])
        total = 0
        for system, level in branch['recommended_mastery'].items():
            if not 1 <= level <= 4 or system not in unlocks:
                errors.append('Invalid recommended mastery: ' + branch['id'])
                continue
            total += sum(mastery[system, step][0] for step in range(2, level + 1))
            if unlocks[system] > branch['min_realm']:
                errors.append('Branch recommends a locked activity: ' + branch['id'])
        if total != branch['early_insight_budget_check']:
            errors.append('Mastery budget changed: ' + branch['id'] + f' expects {total:g}')
        if any(action not in actions for action in branch['actions']):
            errors.append('Branch references an unknown action: ' + branch['id'])
    for goal in goals.values():
        if goal['id'] not in route_text:
            errors.append('Goal missing from player route document: ' + goal['id'])
        if any(dependency not in goals for dependency in goal['requires']):
            errors.append('Unknown route dependency: ' + goal['id'])
        if goal.get('region_id') and goal['region_id'] not in regions:
            errors.append('Unknown route region: ' + goal['id'])
        if goal.get('required_mastery') or goal.get('required_branches') or goal.get('required_actions'):
            errors.append('Main goals cannot require optional deepening or full mastery: ' + goal['id'])
    visiting, visited = set(), set()

    def walk(ident):
        if ident in visiting:
            errors.append('Cyclic route dependency: ' + ident)
            return
        if ident in visited or ident not in goals:
            return
        visiting.add(ident)
        for dependency in goals[ident]['requires']:
            walk(dependency)
        visiting.remove(ident)
        visited.add(ident)

    for ident in goals:
        walk(ident)
    for i in range(1, 6):
        ident = f'route_clear_{i:03d}'
        expected = [] if i == 1 else [f'route_clear_{i-1:03d}']
        goal = goals.get(ident, {})
        if goal.get('requires') != expected or goal.get('region_id') != f'lv_qi_{i:03d}':
            errors.append('Region chain is not a direct five-region route: ' + ident)
    for ident in ('route_qi_complete', 'route_foundation'):
        if goals.get(ident, {}).get('requires') != ['route_clear_005']:
            errors.append('Chapter endpoint requires unrelated optional content: ' + ident)
    if goals.get('route_foundation', {}).get('min_realm') != 10:
        errors.append('Foundation endpoint must represent the actual realm 9 to 10 boundary')
    return errors


def main():
    data = read_json(DESIGN / '23_deepening_routes.json')
    errors = validate(data)
    if errors:
        raise SystemExit('\n'.join(errors))
    print(f"DESIGN VALIDATION: {len(data['systems'])} activities, {len(data['actions'])} operations, {len(data['milestones'])} milestones, {len(data['branches'])} optional branches; recipe IDs, unlocks, route DAG and mastery budgets consistent.")
    print('Scope: document consistency only; combat, timing and economic balance require implementation/playtests.')


if __name__ == '__main__':
    main()
