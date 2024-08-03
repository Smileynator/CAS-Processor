# CAS-CANM-Processor
 Processes CAS and CANM files for Earth Defense Force 5 and 6.

## Usage
 - Drop a CAS file on the application to extract a CANM file of the same name.
 - Drop a CANM file on the application to replace the CANM file inside the CAS file of the same name.

 It can deal with differently sized CANM files to transplant inside the CAS file. However it only does basic sanity checks for file signatures and versions.

 Note that it does not prompt for overrides, it assumes you know your order of operations.
 Meaning it will gladly override your already existing CANM file if you re-drag the CAS file.
 
 Note that mixing and matching EDF 5 and 6 files is unlikely to turn out well, the different internal version numbers are likely to not work out when the game loads the files.

## License
EARTH DEFENSE FORCE is the registered trademark of SANDLOT and D3 PUBLISHER INC. This project is not affiliated with or endorsed by SANDLOT or D3 PUBLISHER INC in any way.

This work is licensed under a [Creative Commons Attribution-NonCommercial 4.0 International License](https://creativecommons.org/licenses/by-nc/4.0/) (CC BY-NC 4.0).
