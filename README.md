# Show Players Everywhere

This is a single-hook mod that I made because the map was more useless than I expected it to be.

### Functionality:
* IF the map is being drawn (map.fade > 0), it checks for all players in the owner's region and stores their colors.
* IF Rain Meadow is enabled and the player is in a lobby, it also asks Rain Meadow for a list of players and attempts to get their base colors.
* These two lists are combined, (and I attempted to prevent the owner himself from being added, but sometimes he gets added anyway).
* Loops through each player in the list and draws his symbol to the map. Pretty simple.
* Adds the players to the list of creatureSymbols, so the vanilla map code should clear them when needed.