All the web stuff goes here pls


API: 



- Unity sends:
    - Create Game: (this also handles the creation, returns game ID)
        - Amount of Players

    - Create Round: (Returns each players input, like when they pressed how many times etc after timer expires)
        - Game ID
        - Timer Limit

    - End Game: (Save stuff to our DB)
        - Game ID