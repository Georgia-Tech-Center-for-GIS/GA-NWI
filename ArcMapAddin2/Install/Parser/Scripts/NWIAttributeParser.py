"""
Tool Name:  NWI Attribute Parser
Source Name: NWIAttributeParser.py
Version: ArcGIS 10.0
Author: Thai Truong
"""

####################################################
import os, sys, arcpy

#####################################################
liFieldNames = ["LMiIndex", "LMiZScore", "LMiPValue", "COType"]

#####################################################
def MainApp():
    """Retrieves the parameters from the User Interface and executes the
    appropriate commands."""

    inputFC = arcpy.GetParameterAsText(0)
    AttributeField = arcpy.GetParameterAsText(1)
    outputFC = arcpy.GetParameterAsText(2)
    overwrite = arcpy.GetParameterAsText(3)
    arcpy.overwriteOutput = True

    arcpy.AddMessage("\nScript detects you are using: " + arcpy.ProductInfo() +
        ", version " + arcpy.GetInstallInfo("desktop").get("Version"))

    arcpy.AddMessage("Making a copy of Input Feature Class")
    arcpy.CopyFeatures_management(inputFC, outputFC)

    dupFields = CheckFields(outputFC)

    if overwrite == "false" and len(dupFields) > 0:
        arcpy.AddMessage ("You need to re-name the following field(s): " +
            ', '.join(column for column in dupFields)
            + " before re-running this tool.")
        arcpy.AddMessage("Deleting the Output Feature Class... ")
        arcpy.DeleteFeatures_management(outputFC)

    elif (overwrite == "false" and len(dupFields) < 1) or overwrite == "true":
        if len(dupFields) > 0:
            arcpy.AddMessage("Deleting existing field(s): " +
                ', '.join(column for column in dupFields))
            # Execute DeleteField
            arcpy.DeleteField_management(outputFC, dupFields)
        fields = [
            ("SYSTEM","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("SUBSYSTEM","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("CLASS1","TEXT","","","2","","NULLABLE","NON_REQUIRED",""),
            ("SUBCLASS1","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("CLASS2","TEXT","","","2","","NULLABLE","NON_REQUIRED",""),
            ("SUBCLASS2","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("WATER1","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("WATER2","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("WATER3","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("CHEMISTRY1","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("CHEMISTRY2","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("SOIL","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("SPECIAL1","TEXT","","","1","","NULLABLE","NON_REQUIRED",""),
            ("SPECIAL2","TEXT","","","1","","NULLABLE","NON_REQUIRED","")
            ]

        arcpy.AddMessage("Adding required fields to Output Feature Class")
        for field in fields:
            arcpy.AddField_management(*(outputFC,) + field)

        arcpy.AddMessage("Getting unique values of NWI codes")
        AttributeValues = getUniqueAttribute(outputFC,AttributeField)
        arcpy.AddMessage("Parsing out each unique values of NWI code")
        headers = ["NWICODE", "SYSTEM", "SUBSYSTEM", "CLASS1", "SUBCLASS1",
            "CLASS2", "SUBCLASS2", "WATER1", "WATER2", "WATER3", "CHEMISTRY1",
            "CHEMISTRY2", "SOIL", "SPECIAL1", "SPECIAL2"]
        table = []
        arcpy.AddMessage(''.join(column.rjust(11) for column in headers))

        TotalAttributes = len(AttributeValues)

        # Set the progressor
        #
        arcpy.SetProgressor("step", "Parsing out attributes...", 0,TotalAttributes, 1)

        for item in AttributeValues:
            # Update the progressor label for current shapefile
            #
            arcpy.SetProgressorLabel("Parsing out attributes: " + item + "...")
            table.append(item)
            if item[:1] == "E":
                Attributes = Estuarine(item)
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)

            elif item[:1] == "L":
                Attributes = Lacustrine(item)
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)

            elif item[:1] == "M":
                Attributes = Marine(item)
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)

            elif item[:1] == "P":
                Attributes = Palustrine(item)
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)

            elif item[:1] == "R":
                Attributes = Riverine(item)
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)
            else:
                Attributes = ["U","","","","","","","","","","","","",""]
                table.extend(Attributes)
                arcpy.AddMessage(''.join(column.rjust(11) for column in table))
                CalculateField(outputFC,AttributeField,item,Attributes)
            del table[:]
            # Update the progressor position
            #
            arcpy.SetProgressorPosition()
        arcpy.ResetProgressor()
        arcpy.AddMessage("\n")

#######################################################################
def CheckFields(outputFC):
    newFields = ["SYSTEM", "SUBSYSTEM", "CLASS1", "SUBCLASS1", "CLASS2",
        "SUBCLASS2", "WATER1", "WATER2", "WATER3", "CHEMISTRY1", "CHEMISTRY2",
        "SOIL", "SPECIAL1", "SPECIAL2"]
    existingFields = []
    fieldList = arcpy.ListFields(outputFC)
    for field in fieldList:
        if field.name.upper() in newFields:
            existingFields.append(field.name)
    return existingFields

def getUniqueAttribute (inputTable, field):
    values = set() # set to hold unique values
    Version = arcpy.GetInstallInfo("desktop").get("Version")
    # Use Search Cursors for Desktop version 10.0 or older
    #
    from distutils.version import StrictVersion
    
    if StrictVersion(Version) <= StrictVersion('10.0'):
        rows = arcpy.SearchCursor(inputTable)
        for row in rows:
            values.add(row.getValue(field))
        return sorted(values)
    # Use new Data Access module for Desktop version 10.1 or later
    #
    else:
        with arcpy.da.SearchCursor(inputTable, (field,)) as cursor:
            for row in cursor:
                values.add(row[0])
        return sorted(values)

def Estuarine(ecode):
    eSubsystemDict = {'E1': '1', 'E2': '2'}
    eClassDict = {'AB': 'AB', 'EM': 'EM' , 'FO': 'FO', 'RB': 'RB' , 'RF': 'RF',
        'RS': 'RS' , 'SB': 'SB', 'SS': 'SS' , 'UB': 'UB', 'US': 'US'}
    eSubclassDict = {'RB1': '1', 'RB2': '2', 'UB1': '1', 'UB2': '2', 'UB3': '3',
        'UB4': '4', 'AB1': '1', 'AB3': '3', 'AB4': '4', 'RF2': '2', 'RF3': '3',
        'SB1': '1', 'SB2': '2', 'SB3': '3', 'SB4': '4', 'SB5': '5', 'SB6': '6',
        'RS1': '1', 'RS2': '2', 'US1': '1', 'US2': '2', 'US3': '3', 'US4': '4',
        'EM1': '1', 'EM2': '2', 'EM5': '5', 'SS1': '1', 'SS2': '2', 'SS3': '3',
        'SS4': '4', 'SS5': '5', 'SS6': '6', 'SS7': '7', 'FO1': '1', 'FO2': '2',
        'FO3': '3', 'FO4': '4', 'FO5': '5', 'FO6': '6', 'FO7': '7',}

    System = "E"
    if ecode.find('/') == 4:
        Subsystem = eSubsystemDict.get(ecode[:2])
        Class1 = eClassDict.get(ecode[2:4])
        Class2 = eClassDict.get(ecode[5:7])
        Modifiers = ecode[7:]

        if len(ecode)> 7 and (ecode[7]).isdigit():
            Subclass1 = ""
            Subclass2 = eSubclassDict.get(ecode[5:8])
            Modifiers = ecode[8:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            Modifiers = ecode[7:]

    elif ecode.find('/') == 5:
        Subsystem = eSubsystemDict.get(ecode[:2])

        if ecode[6].isdigit():
            Class1 = eClassDict.get(ecode[2:4])
            Class2 = eClassDict.get(ecode[2:4])
            Subclass1 = eSubclassDict.get(ecode[2:5])
            Subclass2 = eSubclassDict.get(ecode[2:4] + ecode[6])
            Modifiers = ecode[7:]
        else:
            Class1 = eClassDict.get(ecode[2:4])
            Class2 = eClassDict.get(ecode[6:8])
            if ecode[8].isdigit():
                Subclass1 = eSubclassDict.get(ecode[2:5])
                Subclass2 = eSubclassDict.get(ecode[6:9])
                Modifiers = ecode[9:]
            else:
                Subclass1 = eSubclassDict.get(ecode[2:5])
                Subclass2 = ""
                Modifiers = ecode[8:]
    else:
        Subsystem = eSubsystemDict.get(ecode[:2])
        Class1 = eClassDict.get(ecode[2:4])
        Class2 = ""
        if len(ecode)>4 and ecode[4].isdigit():
            Subclass1 = eSubclassDict.get(ecode[2:5])
            Subclass2 = ""
            Modifiers = ecode[5:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            if len(ecode)> 4:
                Modifiers = ecode[4:]
            else:
                Modifiers = ""

    Subsystem = str(Subsystem).replace("None",  "")
    Class1 =  str(Class1).replace("None",  "")
    Class2 =  str(Class2).replace("None",  "")
    Subclass1 = str(Subclass1).replace("None",  "")
    Subclass2 = str(Subclass2).replace("None",  "")

    Water = getWaterRegime(Modifiers)
    Water1 = Water[0]
    Water2 = Water[1]
    Water3 = Water[2]

    Special = getSpecialModifiers(Modifiers)
    Special1 = Special[0]
    Special2 = Special[1]

    Chemistry = getWaterChemistry(Modifiers)
    Chemistry1 = Chemistry[0]
    Chemistry2 = Chemistry[1]

    Soil = getSoil(Modifiers)

    return [System, Subsystem, Class1, Subclass1, Class2, Subclass2, Water1,
        Water2, Water3, Chemistry1, Chemistry2, Soil, Special1, Special2]

def Lacustrine(lcode):
    lSubsystemDict = {'L1': '1', 'L2': '2'}
    lClassDict = {'RB': 'RB', 'UB': 'UB' , 'AB': 'AB', 'RS': 'RS' , 'US': 'US',
        'EM': 'EM'}
    lSubclassDict = {'RB1': '1', 'RB2': '2', 'UB1': '1', 'UB2': '2', 'UB3': '3',
        'UB4': '4', 'AB1': '1', 'AB2': '2', 'AB3': '3', 'AB4': '4', 'RS1': '1',
        'RS2': '2', 'US1': '1', 'US2': '2', 'US3': '3', 'US4': '4', 'US5': '5',
        'EM2': '2'}

    System = "L"
    if lcode.find('/') == 4:
        Subsystem = lSubsystemDict.get(lcode[:2])
        Class1 = lClassDict.get(lcode[2:4])
        Class2 = lClassDict.get(lcode[5:7])
        Modifiers = lcode[7:]

        if len(lcode)> 7 and (lcode[7]).isdigit():
            Subclass1 = ""
            Subclass2 = lSubclassDict.get(lcode[5:8])
            Modifiers = lcode[8:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            Modifiers = lcode[7:]

    elif lcode.find('/') == 5:
        Subsystem = lSubsystemDict.get(lcode[:2])

        if lcode[6].isdigit():
            Class1 = lClassDict.get(lcode[2:4])
            Class2 = lClassDict.get(lcode[2:4])
            Subclass1 = lSubclassDict.get(lcode[2:5])
            Subclass2 = lSubclassDict.get(lcode[2:4] + lcode[6])
            Modifiers = lcode[7:]
        else:
            Class1 = lClassDict.get(lcode[2:4])
            Class2 = lClassDict.get(lcode[6:8])
            if lcode[8].isdigit():
                Subclass1 = lSubclassDict.get(lcode[2:5])
                Subclass2 = lSubclassDict.get(lcode[6:9])
                Modifiers = lcode[9:]
            else:
                Subclass1 = lSubclassDict.get(lcode[2:5])
                Subclass2 = ""
                Modifiers = lcode[8:]
    else:
        Subsystem = lSubsystemDict.get(lcode[:2])
        Class1 = lClassDict.get(lcode[2:4])
        Class2 = ""
        if len(lcode)>4 and lcode[4].isdigit():
            Subclass1 = lSubclassDict.get(lcode[2:5])
            Subclass2 = ""
            Modifiers = lcode[5:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            if len(lcode)> 4:
                Modifiers = lcode[4:]
            else:
                Modifiers = ""

    Subsystem = str(Subsystem).replace("None",  "")
    Class1 =  str(Class1).replace("None",  "")
    Class2 =  str(Class2).replace("None",  "")
    Subclass1 = str(Subclass1).replace("None",  "")
    Subclass2 = str(Subclass2).replace("None",  "")

    Water = getWaterRegime(Modifiers)
    Water1 = Water[0]
    Water2 = Water[1]
    Water3 = Water[2]

    Special = getSpecialModifiers(Modifiers)
    Special1 = Special[0]
    Special2 = Special[1]

    Chemistry = getWaterChemistry(Modifiers)
    Chemistry1 = Chemistry[0]
    Chemistry2 = Chemistry[1]

    Soil = getSoil(Modifiers)

    return [System, Subsystem, Class1, Subclass1, Class2, Subclass2, Water1,
        Water2, Water3, Chemistry1, Chemistry2, Soil, Special1, Special2]

def Marine(mcode):
    mSubsystemDict = {'M1': '1', 'M2': '2'}
    mClassDict = {'RB': 'RB', 'UB': 'UB' , 'AB': 'AB', 'RF': 'RF', 'RS': 'RS',
        'US': 'US'}
    mSubclassDict = {'RB1': '1', 'RB2': '2', 'UB1': '1', 'UB2': '2', 'UB3': '3',
        'AB1': '1', 'AB3': '3', 'RF1': '1', 'RF3': '3', 'RS1': '1', 'RS2': '2',
        'US1': '1', 'US2': '2', 'US3': '3', 'US4': '4'}

    System = "M"

    if mcode.find('/') == 4:
        Subsystem = mSubsystemDict.get(mcode[:2])
        Class1 = mClassDict.get(mcode[2:4])
        Class2 = mClassDict.get(mcode[5:7])
        Modifiers = mcode[7:]

        if len(mcode)> 7 and (mcode[7]).isdigit():
            Subclass1 = ""
            Subclass2 = mSubclassDict.get(mcode[5:8])
            Modifiers = mcode[8:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            Modifiers = mcode[7:]

    elif mcode.find('/') == 5:
        Subsystem = mSubsystemDict.get(mcode[:2])

        if mcode[6].isdigit():
            Class1 = mClassDict.get(mcode[2:4])
            Class2 = mClassDict.get(mcode[2:4])
            Subclass1 = mSubclassDict.get(mcode[2:5])
            Subclass2 = mSubclassDict.get(mcode[2:4] + mcode[6])
            Modifiers = mcode[7:]
        else:
            Class1 = mClassDict.get(mcode[2:4])
            Class2 = mClassDict.get(mcode[6:8])
            if mcode[8].isdigit():
                Subclass1 = mSubclassDict.get(mcode[2:5])
                Subclass2 = mSubclassDict.get(mcode[6:9])
                Modifiers = mcode[9:]
            else:
                Subclass1 = mSubclassDict.get(mcode[2:5])
                Subclass2 = ""
                Modifiers = mcode[8:]
    else:
        Subsystem = mSubsystemDict.get(mcode[:2])
        Class1 = mClassDict.get(mcode[2:4])
        Class2 = ""
        if len(mcode)>4 and mcode[4].isdigit():
            Subclass1 = mSubclassDict.get(mcode[2:5])
            Subclass2 = ""
            Modifiers = mcode[5:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            if len(mcode)> 4:
                Modifiers = mcode[4:]
            else:
                Modifiers = ""

    Subsystem = str(Subsystem).replace("None",  "")
    Class1 =  str(Class1).replace("None",  "")
    Class2 =  str(Class2).replace("None",  "")
    Subclass1 = str(Subclass1).replace("None",  "")
    Subclass2 = str(Subclass2).replace("None",  "")

    Water = getWaterRegime(Modifiers)
    Water1 = Water[0]
    Water2 = Water[1]
    Water3 = Water[2]

    Special = getSpecialModifiers(Modifiers)
    Special1 = Special[0]
    Special2 = Special[1]

    Chemistry = getWaterChemistry(Modifiers)
    Chemistry1 = Chemistry[0]
    Chemistry2 = Chemistry[1]

    Soil = getSoil(Modifiers)

    return [System, Subsystem, Class1, Subclass1, Class2, Subclass2, Water1,
        Water2, Water3, Chemistry1, Chemistry2, Soil, Special1, Special2]

def Palustrine(pcode):
    pClassDict = {'RB': 'RB', 'UB': 'UB', 'AB': 'AB', 'US': 'US', 'ML': 'ML',
        'EM': 'EM', 'SS': 'SS', 'FO': 'FO'}
    pSubclassDict = {'RB1': '1', 'RB2': '2', 'UB1': '1', 'UB2': '2', 'UB3': '3',
        'UB4': '4', 'AB1': '1', 'AB2': '2', 'AB3': '3', 'AB4': '4', 'US1': '1',
        'US2': '2', 'US3': '3', 'US4': '4', 'US5': '5', 'ML1': '1', 'ML2': '2',
        'EM1': '1', 'EM2': '2', 'EM5': '5', 'SS1': '1', 'SS2': '2', 'SS3': '3',
        'SS4': '4', 'SS5': '5', 'SS6': '6', 'SS7': '7', 'FO1': '1', 'FO2': '2',
        'FO3': '3', 'FO4': '4', 'FO5': '5', 'FO6': '6', 'FO7': '7'}

    System = "P"
    Subsystem = ""

    if pcode.find('/') == 3:
        Class1 = pClassDict.get(pcode[1:3])
        Class2 = pClassDict.get(pcode[4:6])
        Modifiers = pcode[6:]

        if len(pcode)> 6 and (pcode[6]).isdigit():
            Subclass1 = ""
            Subclass2 = pSubclassDict.get(pcode[4:7])
            Modifiers = pcode[7:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            Modifiers = pcode[6:]

    elif pcode.find('/') == 4:
        if pcode[5].isdigit():
            Class1 = pClassDict.get(pcode[1:3])
            Class2 = pClassDict.get(pcode[1:3])
            Subclass1 = pSubclassDict.get(pcode[1:4])
            Subclass2 = pSubclassDict.get(pcode[1:3] + pcode[5])
            Modifiers = pcode[6:]
        else:
            Class1 = pClassDict.get(pcode[1:3])
            Class2 = pClassDict.get(pcode[5:7])
            if len(pcode) > 7 and pcode[7].isdigit():
                Subclass1 = pSubclassDict.get(pcode[1:4])
                Subclass2 = pSubclassDict.get(pcode[5:8])
                Modifiers = pcode[8:]
            else:
                Subclass1 = pSubclassDict.get(pcode[1:4])
                Subclass2 = ""
                Modifiers = pcode[7:]
    else:
        Class1 = pClassDict.get(pcode[1:3])
        Class2 = ""
        if len(pcode)>3 and pcode[3].isdigit():
            Subclass1 = pSubclassDict.get(pcode[1:4])
            Subclass2 = ""
            Modifiers = pcode[4:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            if len(pcode) <= 3:
                Modifiers = pcode[1:]
            elif len(pcode)> 3:
                Modifiers = pcode[3:]
            else:
                Modifiers = ""

    Subsystem = str(Subsystem).replace("None",  "")
    Class1 =  str(Class1).replace("None",  "")
    Class2 =  str(Class2).replace("None",  "")
    Subclass1 = str(Subclass1).replace("None",  "")
    Subclass2 = str(Subclass2).replace("None",  "")

    Water = getWaterRegime(Modifiers)
    Water1 = Water[0]
    Water2 = Water[1]
    Water3 = Water[2]

    Special = getSpecialModifiers(Modifiers)
    Special1 = Special[0]
    Special2 = Special[1]

    Chemistry = getWaterChemistry(Modifiers)
    Chemistry1 = Chemistry[0]
    Chemistry2 = Chemistry[1]

    Soil = getSoil(Modifiers)

    return [System, Subsystem, Class1, Subclass1, Class2, Subclass2, Water1,
        Water2, Water3, Chemistry1, Chemistry2, Soil, Special1, Special2]

def Riverine(rcode):
    rSubsystemDict = {'R1': '1', 'R2': '2', 'R3': '3', 'R4': '4', 'R5': '5'}
    rClassDict = {'1RB': 'RB', '3RB': 'RB', '1UB': 'UB', '2UB': 'UB',
        '3UB': 'UB', '5UB': 'UB', '1SB': 'SB', '4SB': 'SB', '1AB': 'AB',
        '2AB': 'AB', '3AB': 'AB', '1RS': 'RS', '2RS': 'RS', '3RS': 'RS',
        '1US': 'US', '2US': 'US', '3US': 'US', '1EM': 'EM', '2EM': 'EM',
        '3EM': 'EM'}
    rSubclassDict = {'RB1': '1', 'RB2': '2', 'UB1': '1', 'UB2': '2',
        'UB3': '3', 'UB4': '4', 'SB1': '1', 'SB2': '2', 'SB3': '3', 'SB4': '4',
        'SB5': '5', 'SB6': '6', 'SB7': '7', 'AB1': '1', 'AB2': '2', 'AB3': '3',
        'AB4': '4', 'RS1': '1', 'RS2': '2', 'US1': '1', 'US2': '2', 'US3': '3',
        'US4': '4', 'US5': '5', 'EM2': '2'}

    System = "R"

    if rcode.find('/') == 4:
        Subsystem = rSubsystemDict.get(rcode[:2])
        Class1 = rClassDict.get(rcode[1:4])
        Class2 = rClassDict.get(rcode[1] + rcode[5:7])
        Modifiers = rcode[7:]

        if len(rcode)> 7 and (rcode[7]).isdigit():
            Subclass1 = ""
            Subclass2 = rSubclassDict.get(rcode[5:8])
            Modifiers = rcode[8:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            Modifiers = rcode[7:]

    elif rcode.find('/') == 5:
        Subsystem = rSubsystemDict.get(rcode[:2])

        if rcode[6].isdigit():
            Class1 = rClassDict.get(rcode[1:4])
            Class2 = rClassDict.get(rcode[1:4])
            Subclass1 = rSubclassDict.get(rcode[2:5])
            Subclass2 = rSubclassDict.get(rcode[2:4] + rcode[6])
            Modifiers = rcode[7:]
        else:
            Class1 = rClassDict.get(rcode[1:4])
            Class2 = rClassDict.get(rcode[1] + rcode[6:8])
            if rcode[8].isdigit():
                Subclass1 = rSubclassDict.get(rcode[2:5])
                Subclass2 = rSubclassDict.get(rcode[6:9])
                Modifiers = rcode[9:]
            else:
                Subclass1 = rSubclassDict.get(rcode[2:5])
                Subclass2 = ""
                Modifiers = rcode[8:]
    else:
        Subsystem = rSubsystemDict.get(rcode[:2])
        Class1 = rClassDict.get(rcode[1:4])
        Class2 = ""
        if len(rcode)>4 and rcode[4].isdigit():
            Subclass1 = rSubclassDict.get(rcode[2:5])
            Subclass2 = ""
            Modifiers = rcode[5:]
        else:
            Subclass1 = ""
            Subclass2 = ""
            if len(rcode)> 4:
                Modifiers = rcode[4:]
            else:
                Modifiers = ""

    Subsystem = str(Subsystem).replace("None",  "")
    Class1 =  str(Class1).replace("None",  "")
    Class2 =  str(Class2).replace("None",  "")
    Subclass1 = str(Subclass1).replace("None",  "")
    Subclass2 = str(Subclass2).replace("None",  "")

    Water = getWaterRegime(Modifiers)
    Water1 = Water[0]
    Water2 = Water[1]
    Water3 = Water[2]

    Special = getSpecialModifiers(Modifiers)
    Special1 = Special[0]
    Special2 = Special[1]

    Chemistry = getWaterChemistry(Modifiers)
    Chemistry1 = Chemistry[0]
    Chemistry2 = Chemistry[1]

    Soil = getSoil(Modifiers)

    return [System, Subsystem, Class1, Subclass1, Class2, Subclass2, Water1,
        Water2, Water3, Chemistry1, Chemistry2, Soil, Special1, Special2]

def getWaterRegime(Wvalue):
    WaterRegime1 = ('A', 'B', 'C', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N',
        'P', 'S', 'R', 'T', 'V')
    WaterRegime2 = list(Wvalue)
    WaterRegimeList = list(set(WaterRegime1).intersection(set(WaterRegime2)))

    if len(WaterRegimeList) == 3:
        Water1 = WaterRegimeList[0]
        Water2 = WaterRegimeList[1]
        Water3 = WaterRegimeList[2]
    elif len(WaterRegimeList) == 2:
        Water1 = WaterRegimeList[0]
        Water2 = WaterRegimeList[1]
        Water3 = ""
    elif len(WaterRegimeList) == 1:
        Water1 = WaterRegimeList[0]
        Water2 = ""
        Water3 = ""
    else:
        Water1 = ""
        Water2 = ""
        Water3 = ""
    return Water1, Water2, Water3

def getSpecialModifiers(Svalue):
    SpecialModifiers1 = ('b','d','f','h','r','s','x')
    SpecialModifiers2 = list(Svalue)
    SpecialModifiersList = list(set(SpecialModifiers1).intersection(set(SpecialModifiers2)))

    if len(SpecialModifiersList) == 2:
        Special1 = SpecialModifiersList[0]
        Special2 = SpecialModifiersList[1]
    elif len(SpecialModifiersList) == 1:
        Special1 = SpecialModifiersList[0]
        Special2 = ""
    else:
        Special1 = ""
        Special2 = ""
    return Special1, Special2

def getWaterChemistry(Cvalue):
    WaterChemistry1 = ('0','1','2','3','4','5','6','7','8','9','a','t','i')
    WaterChemistry2 = list(Cvalue)
    WaterChemistryList = list(set(WaterChemistry1).intersection(set(WaterChemistry2)))

    if len(WaterChemistryList) == 2:
        Chemistry1 = WaterChemistryList[0]
        Chemistry2 = WaterChemistryList[1]
    elif len(WaterChemistryList) == 1:
        Chemistry1 = WaterChemistryList[0]
        Chemistry2 = ""
    else:
        Chemistry1 = ""
        Chemistry2 = ""
    return Chemistry1, Chemistry2

def getSoil(Sovalue):
    Soil1 = ('g','n')
    Soil2 = list(Sovalue)
    SoilList = list(set(Soil1).intersection(set(Soil2)))

    if len(SoilList) == 1:
        Soil = SoilList[0]
    else:
        Soil = ""
    return Soil

def CalculateField (outFC, NWIField, cValue, Attributes):
    desc = arcpy.Describe(outFC)
    if desc.path[-3:].lower() == "mdb":
        where_clause = "[" + NWIField + "] = '" + cValue + "'"
    else:
        where_clause = "\"" + NWIField + "\" = '" + cValue + "'"
    Version = arcpy.GetInstallInfo("desktop").get("Version")
    if float(Version) <= 10.0:
        # Create update cursor for feature class
        #
        rows = arcpy.UpdateCursor(outFC, where_clause)

        # Update the fields based on the NWI Code
        #
        for row in rows:
            row.SYSTEM = Attributes[0]
            row.SUBSYSTEM = Attributes[1]
            row.CLASS1 = Attributes[2]
            row.SUBCLASS1 = Attributes[3]
            row.CLASS2 = Attributes[4]
            row.SUBCLASS2 = Attributes[5]
            row.WATER1 = Attributes[6]
            row.WATER2 = Attributes[7]
            row.WATER3 = Attributes[8]
            row.CHEMISTRY1 = Attributes[9]
            row.CHEMISTRY2 = Attributes[10]
            row.SOIL = Attributes[11]
            row.SPECIAL1 = Attributes[12]
            row.SPECIAL2 = Attributes[13]
            rows.updateRow(row)

        # Delete cursor and row objects to remove locks on the data
        #
        del row
        del rows
    else:
        UpdateFields = ("SYSTEM", "SUBSYSTEM", "CLASS1", "SUBCLASS1", "CLASS2",
            "SUBCLASS2", "WATER1", "WATER2", "WATER3", "CHEMISTRY1",
            "CHEMISTRY2", "SOIL", "SPECIAL1", "SPECIAL2")
        # Create the update cursor and update each row returned by the SQL expression
        #
        with arcpy.da.UpdateCursor(outFC, UpdateFields, where_clause) as cursor:
            for row in cursor:
                row[0] = Attributes[0]
                row[1] = Attributes[1]
                row[2] = Attributes[2]
                row[3] = Attributes[3]
                row[4] = Attributes[4]
                row[5] = Attributes[5]
                row[6] = Attributes[6]
                row[7] = Attributes[7]
                row[8] = Attributes[8]
                row[9] = Attributes[9]
                row[10] = Attributes[10]
                row[11] = Attributes[11]
                row[12] = Attributes[12]
                row[13] = Attributes[13]
                cursor.updateRow(row)

if __name__ == "__main__":
    MainApp()





