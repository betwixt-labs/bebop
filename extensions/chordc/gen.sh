#!/bin/bash

# Check if a JSON file and a C# file are provided
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <json_file> <cs_file>"
    exit 1
fi

json_file="$1"
cs_file="$2"

# Generate the C# code from the JSON file and save it to a temp file
temp_code_file="temp_generated_code.cs"
{
    echo '        _signatureLookup = new Dictionary<string, List<FunctionSignature>>()'
    echo '        {'
    jq -r '. as $in | keys[] | . as $compiler | 
        "\t\t\t\t{\n\t\t\t\t\t\""+$compiler+"\",\n\t\t\t\t\tnew ()\n\t\t\t\t\t{\n" + 
        ($in[$compiler] | to_entries | map(
            "\t\t\t\t\t\tnew (\""+.key+"\",\n\t\t\t\t\t\t\t[\n\t\t\t\t\t\t\t\t" + 
            ( [.value.parameters[] | 
                if . == "i32" then "WebAssemblyValueType.Int32" 
                elif . == "i64" then "WebAssemblyValueType.Int64" 
                elif . == "f32" then "WebAssemblyValueType.Float32" 
                elif . == "f64" then "WebAssemblyValueType.Float64" 
                else . end
            ] | join(",\n\t\t\t\t\t\t\t\t") ) + 
            "\n\t\t\t\t\t\t\t],\n\t\t\t\t\t\t\t[\n\t\t\t\t\t\t\t\t" + 
            (if .value.returns then 
                ([.value.returns[] | 
                    if . == "i32" then "WebAssemblyValueType.Int32" 
                    elif . == "i64" then "WebAssemblyValueType.Int64" 
                    elif . == "f32" then "WebAssemblyValueType.Float32" 
                    elif . == "f64" then "WebAssemblyValueType.Float64" 
                    else . end
                ] | join(",\n\t\t\t\t\t\t\t\t")) 
            else "" end) + 
            "\n\t\t\t\t\t\t\t]\n\t\t\t\t\t\t)"
        ) | join(",\n\n") ) + 
        "\n\t\t\t\t\t}\n\t\t\t\t},"'
    echo '         };'
} <"$json_file" >"$temp_code_file"

# Use awk to insert the generated code into the static constructor
awk -v temp_file="$temp_code_file" '
    /\/\/ Begin generated code/ {
        print
        while((getline line < temp_file) > 0) {
            print line
        }
        close(temp_file)
        skip = 1
        next
    }
    /\/\/ End generated code/ {
        print
        skip = 0
        next
    }
    skip {next}
    {print}
' "$cs_file" >tmp_file && mv tmp_file "$cs_file"

# Clean up temp file
rm "$temp_code_file"
# Format the C# code
dotnet format --include "$cs_file"
