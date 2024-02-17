# CAS-CANM-Processor
 Processes CAS and CANM files for Earth Defense Force 5.

## Usage
 - Drop a CAS file on the application to extract a CANM file of the same name.
 - Drop a CANM file on the application to replace the CANM file inside the CAS file of the same name.

 It can deal with differently sized CANM files to transplant inside the CAS file. However it only does basic sanity checks for file signatures and versions.

 Note that it does not prompt for overrides, it assumes you know your order of operations.
 Meaning it will gladly override your already existing CANM file if you re-drag the CAS file.
