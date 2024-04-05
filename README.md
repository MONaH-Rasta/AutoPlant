# AutoPlant

Oxide plugin for Rust. Automation of your plantations.
Players with permission are able to plant whole planterbox in one click. To do that you should:

1. Select plant/clone/seed to be your active item
2. Target planterbox
3. **HOLD SHIFT (SPRINT button)** and click mouse button as you usually plant
4. All free slots in planterbox will be planted by active item (if you have enough items in active stack).

Also your players can gather / cutting / remove dying using the **SHIFT (SPRINT button)** key.

To move fertilizer automatically, just set active item and click mouse button.

## Permissions

### Default permissions

- `autoplant.use` -- permission to use auto planting
- `autoplant.gather.use` -- permission to use auto gather
- `autoplant.cutting.use` -- permission to use auto cutting
- `autoplant.removedying.use` -- permission to use auto remove dying
- `autoplant.fertilizer.use` -- permission to use auto move fertilizer

## Chat Commands

- `/fertilizer` -- Shows amount move fertilizer
- `/fertilizer <amount>` -- Set amount move fertilizer

## Configuration

### Default Configuration

```json  
{
  "Auto Plant permission": "autoplant.use",
  "Auto Gather permission": "autoplant.gather.use",
  "Auto Cutting permission": "autoplant.cutting.use",
  "Auto Remove Dying permission": "autoplant.removedying.use",
  "Auto fertilizer permission": "autoplant.fertilizer.use",
  "Auto fertilizer configuration": {
    "Maximum distance from the PlanterBox": 5,
    "Default fertilizer amount": 100
  }
}
```  

## Credits

- **Egor Blagov** - original author

### Special thanks

- **@Sharovoz** - Sponsoring plugin development
