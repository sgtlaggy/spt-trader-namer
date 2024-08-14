# Trader Namer

This mod will let you change traders' nicknames and avatars.

The novel difference from other avatar-swapping mods is that it uses trader nicknames instead of obscure avatar IDs that don't match the trader ID.

Both features of the mod should work for custom traders as long as this mod loads after them.

## Setting Trader Names
Edit `config/names.json` and add a new line between the `{}` in the format `"Default Name": "New Name"`.

The key (left) should be the trader's default English nickname like `Prapor` or `Therapist`, the value (right) is what you want their new name to be.

Examples:
```json
{
    "Prapor": "Sidorovich"
}
```
```json
{
    "Prapor": "Sidorovich",
    "Fence": "Drebin"
}
```
Details like the trader's Location and Description can also be changed with extended syntax.
Valid keys are "FullName", "FirstName", "Nickname", "Location", and "Description". All are case-sensitive.
```json
{
  "Prapor": {
    "Nickname": "Sidorovich",
    "Location": "Rookie Village in the Cordon",
    "Description": "A reliable source of equipment for stalkers."
  },
  "Fence": "Drebin"
}
```

## Setting Trader Avatars
Name an image based on the trader's default English nickname like `Prapor.jpg` and put it in the mod's `avatars` folder.

Images should be JPEG or PNG and sizes around 500x500 work best.

All users will need to go into their launcher settings and select `Clean Temp Files` for avatars to update.
