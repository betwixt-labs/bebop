package kernel

import (
	"bytes"
	"fmt"
	"strings"

	"github.com/tidwall/gjson"
)

//go:wasm-module chord_tinygo
//export write_line
func kernelWriteLine(s string)

//go:wasm-module chord_tinygo
//export write_error
func kernelWriteError(s string)

//go:wasm-module chord_tinygo
//export get_bebopc_version
func kernelGetBebopcVersion() string

func WriteLine(a interface{}) {
	kernelWriteLine(fmt.Sprintf("%v", a))
}

func WriteError(a interface{}) {
	kernelWriteError(fmt.Sprintf("%v", a))
}

func GetBebopcVersion() string {
	return kernelGetBebopcVersion()
}

func DeserializeContext(context string) GeneratorContext {
	var gc GeneratorContext
	json := gjson.Parse(context)

	gc.Definitions = deserializeDefinitions(json.Get("definitions"))
	gc.Services = deserializeServices(json.Get("services"))
	gc.Constants = deserializeConstants(json.Get("constants"))
	gc.Config = deserializeConfig(json.Get("config"))
	return gc
}

func deserializeConfig(json gjson.Result) GeneratorConfig {
	return GeneratorConfig{
		Alias:            json.Get("alias").String(),
		OutFile:          json.Get("outFile").String(),
		Namespace:        json.Get("namespace").String(),
		EmitNotice:       json.Get("emitNotice").Bool(),
		EmitBinarySchema: json.Get("emitBinarySchema").Bool(),
		Services:         json.Get("services").String(),
		Options:          deserializeOptions(json.Get("options")),
	}
}

func deserializeOptions(json gjson.Result) map[string]string {
	options := make(map[string]string)
	json.ForEach(func(key, value gjson.Result) bool {
		options[key.String()] = value.String()
		return true
	})
	return options
}

func deserializeConstants(json gjson.Result) map[string]ConstantDefinition {
	constants := make(map[string]ConstantDefinition)
	json.ForEach(func(key, value gjson.Result) bool {
		constants[key.String()] = deserializeConstant(value)
		return true
	})
	return constants
}

func deserializeConstant(json gjson.Result) ConstantDefinition {
	return ConstantDefinition{
		Type:  NewBaseType(json.Get("type").String()),
		Value: json.Get("value").String(),
	}
}

func deserializeServices(json gjson.Result) map[string]ServiceDefinition {
	services := make(map[string]ServiceDefinition)
	json.ForEach(func(key, value gjson.Result) bool {
		services[key.String()] = deserializeService(value)
		return true
	})
	return services
}

func deserializeService(json gjson.Result) ServiceDefinition {
	methods := make(map[string]MethodDefinition)
	json.Get("methods").ForEach(func(key, value gjson.Result) bool {
		methods[key.String()] = deserializeMethod(value)
		return true
	})
	return ServiceDefinition{
		Kind:    json.Get("kind").String(),
		Methods: methods,
	}
}

func deserializeMethod(json gjson.Result) MethodDefinition {
	return MethodDefinition{
		Documentation: json.Get("documentation").String(),
		Decorators:    deserializeDecorators(json.Get("decorators")),
		Type:          json.Get("type").String(),
		RequestType:   json.Get("requestType").String(),
		ResponseType:  json.Get("responseType").String(),
		Id:            int(json.Get("id").Int()),
	}
}

func deserializeDefinitions(json gjson.Result) map[string]Definition {
	definitions := make(map[string]Definition)
	json.ForEach(func(key, value gjson.Result) bool {

		kind := value.Get("kind").String()
		switch kind {
		case "enum":
			definitions[key.String()] = deserializeEnum(value)
		case "struct":
			definitions[key.String()] = deserializeStruct(value)
		case "message":
			definitions[key.String()] = deserializeMessage(value)
		case "union":
			definitions[key.String()] = deserializeUnion(value)
		}
		return true
	})
	return definitions
}

func deserializeFields(json gjson.Result) map[string]Field {
	fields := make(map[string]Field)
	json.ForEach(func(key, value gjson.Result) bool {
		fields[key.String()] = deserializeField(value)
		return true
	})
	return fields
}

func deserializeField(json gjson.Result) Field {
	return Field{
		Documentation: json.Get("documentation").String(),
		Decorators:    deserializeDecorators(json.Get("decorators")),
		Type:          deserializeFieldType(json.Get("type")),
		Index:         int(json.Get("index").Int()),
	}
}

func deserializeStruct(json gjson.Result) StructDefinition {
	return StructDefinition{
		FieldsDefinition: FieldsDefinition{
			RecordDefinition: RecordDefinition{
				BaseDefinition: BaseDefinition{
					Kind:          json.Get("kind").String(),
					Documentation: json.Get("documentation").String(),
					Decorators:    deserializeDecorators(json.Get("decorators")),
					Parent:        json.Get("parent").String(),
				},
				minimalEncodedSize:    int(json.Get("minimalEncodedSize").Int()),
				discriminatorInParent: int(json.Get("discriminatorInParent").Int()),
			},
			Fields: deserializeFields(json.Get("fields")),
		},
		Mutable:     json.Get("mutable").Bool(),
		IsFixedSize: json.Get("isFixedSize").Bool(),
	}
}

func deserializeMessage(json gjson.Result) MessageDefinition {
	return MessageDefinition{
		FieldsDefinition: FieldsDefinition{
			RecordDefinition: RecordDefinition{
				BaseDefinition: BaseDefinition{
					Kind:          json.Get("kind").String(),
					Documentation: json.Get("documentation").String(),
					Decorators:    deserializeDecorators(json.Get("decorators")),
					Parent:        json.Get("parent").String(),
				},
				minimalEncodedSize:    int(json.Get("minimalEncodedSize").Int()),
				discriminatorInParent: int(json.Get("discriminatorInParent").Int()),
			},
			Fields: deserializeFields(json.Get("fields")),
		},
	}
}

func deserializeUnion(json gjson.Result) UnionDefinition {
	return UnionDefinition{
		RecordDefinition: RecordDefinition{
			BaseDefinition: BaseDefinition{
				Kind:          json.Get("kind").String(),
				Documentation: json.Get("documentation").String(),
				Decorators:    deserializeDecorators(json.Get("decorators")),
				Parent:        json.Get("parent").String(),
			},
			minimalEncodedSize:    int(json.Get("minimalEncodedSize").Int()),
			discriminatorInParent: int(json.Get("discriminatorInParent").Int()),
		},
		Branches: deserializeUnionBranches(json.Get("branches")),
	}
}

func deserializeUnionBranches(json gjson.Result) map[int]string {
	branches := make(map[int]string)
	json.ForEach(func(key, value gjson.Result) bool {
		branches[int(value.Int())] = key.String()
		return true
	})
	return branches
}

func deserializeFieldType(json gjson.Result) FieldType {
	typeId := json.Get("type").String()
	if typeId == "array" {
		return deserializeArrayType(json.Get("array"))
	} else if typeId == "map" {
		return deserializeMapType(json.Get("map"))
	} else {
		return NewBaseType(typeId)
	}
}

func deserializeArrayType(json gjson.Result) ArrayType {
	return ArrayType{
		Depth:      int(json.Get("depth").Int()),
		MemberType: NewBaseType(json.Get("memberType").String()),
	}
}

func deserializeMapType(json gjson.Result) MapType {
	keyType := json.Get("keyType").String()
	valueType := json.Get("valueType").String()
	if valueType == "array" {
		return MapType{
			KeyType:   NewBaseType(keyType),
			ValueType: deserializeArrayType(json.Get("array")),
		}
	} else if valueType == "map" {
		return MapType{
			KeyType:   NewBaseType(keyType),
			ValueType: deserializeMapType(json.Get("map")),
		}
	}
	return MapType{
		KeyType:   NewBaseType(keyType),
		ValueType: NewBaseType(valueType),
	}
}

func deserializeEnum(json gjson.Result) EnumDefinition {
	return EnumDefinition{
		BaseDefinition: BaseDefinition{
			Kind:          json.Get("kind").String(),
			Documentation: json.Get("documentation").String(),
			Decorators:    deserializeDecorators(json.Get("decorators")),
			Parent:        json.Get("parent").String(),
		},
		IsBitFlags:         json.Get("isBitFlags").Bool(),
		MinimalEncodedSize: int(json.Get("minimalEncodedSize").Int()),
		BaseType:           json.Get("baseType").String(),
		Members:            deserializeEnumMembers(json.Get("members")),
	}
}

func deserializeEnumMembers(json gjson.Result) map[string]EnumMember {
	members := make(map[string]EnumMember)
	json.ForEach(func(key, value gjson.Result) bool {
		members[key.String()] = deserializeEnumMember(value)
		return true
	})
	return members
}

func deserializeEnumMember(json gjson.Result) EnumMember {
	return EnumMember{
		Documentation: json.Get("documentation").String(),
		Decorators:    deserializeDecorators(json.Get("decorators")),
		Value:         json.Get("value").String(),
	}
}

func deserializeDecorators(json gjson.Result) []Decorator {
	var decorators []Decorator
	json.ForEach(func(_, value gjson.Result) bool {
		decorators = append(decorators, deserializeDecorator(value))
		return true
	})
	return decorators
}
func deserializeDecorator(json gjson.Result) Decorator {
	decorator := Decorator{
		Identifier: json.Get("identifier").String(),
		Arguments:  make(map[string]DecoratorArgument),
	}
	arguments := json.Get("arguments")
	if arguments.Exists() {
		arguments.ForEach(func(key, value gjson.Result) bool {
			decorator.Arguments[key.String()] = deserializeDecoratorArgument(value)
			return true
		})
	}
	return decorator
}

func deserializeDecoratorArgument(json gjson.Result) DecoratorArgument {
	return DecoratorArgument{
		Type:  NewBaseType(json.Get("type").String()),
		Value: json.Get("value").String(),
	}
}

type GeneratorContext struct {
	Definitions map[string]Definition
	Services    map[string]ServiceDefinition
	Constants   map[string]ConstantDefinition
	Config      GeneratorConfig
}

func (gc GeneratorContext) GetDefinition(name string) Definition {
	definition, exists := gc.Definitions[name]
	if !exists {
		return nil
	}
	return definition
}

type GeneratorConfig struct {
	Alias            string
	OutFile          string
	Namespace        string
	EmitNotice       bool
	EmitBinarySchema bool
	Services         string
	Options          map[string]string
}

var baseTypes = map[string]bool{
	"byte": true, "uint8": true, "int16": true, "uint16": true, "int32": true,
	"uint32": true, "int64": true, "uint64": true, "float32": true, "float64": true,
	"bool": true, "string": true, "guid": true, "date": true,
}

type BaseType struct {
	Name string
}

func (b BaseType) IsBaseType() bool {
	return baseTypes[b.Name]
}

func (b BaseType) IsFieldType() bool {
	return true
}

func (b BaseType) IsArray() bool {
	return false
}

func (b BaseType) IsMap() bool {
	return false
}

func NewBaseType(name string) BaseType {
	return BaseType{Name: name}
}

type FieldType interface {
	IsFieldType() bool
	IsArray() bool
	IsMap() bool
	IsBaseType() bool
}

type ArrayType struct {
	Depth      int
	MemberType FieldType
}

func (a ArrayType) IsMap() bool {
	return false
}

func (a ArrayType) IsBaseType() bool {
	return false
}

func (a ArrayType) IsFieldType() bool {
	return true
}
func (a ArrayType) IsArray() bool {
	return true
}

type MapType struct {
	KeyType   BaseType
	ValueType FieldType
}

func (m MapType) IsFieldType() bool {
	return true
}
func (m MapType) IsMap() bool {
	return true
}

func (m MapType) IsArray() bool {
	return false
}

func (m MapType) IsBaseType() bool {
	return false
}

type Decorator struct {
	Identifier string
	Arguments  map[string]DecoratorArgument
}

type DecoratorArgument struct {
	Type  BaseType
	Value string
}

type Definition interface {
	Kind() string
}

type BaseDefinition struct {
	Kind          string
	Documentation string
	Decorators    []Decorator
	Parent        string
}

type RecordDefinition struct {
	BaseDefinition
	minimalEncodedSize    int
	discriminatorInParent int
}

type FieldsDefinition struct {
	RecordDefinition
	Fields map[string]Field
}

type StructDefinition struct {
	FieldsDefinition
	Mutable     bool
	IsFixedSize bool
}

func (sd StructDefinition) Kind() string { return "struct" }

type MessageDefinition struct {
	FieldsDefinition
}

func (md MessageDefinition) Kind() string { return "message" }

type EnumDefinition struct {
	BaseDefinition
	IsBitFlags         bool
	MinimalEncodedSize int
	BaseType           string
	Members            map[string]EnumMember
}

func (ed EnumDefinition) Kind() string { return "enum" }

type EnumMember struct {
	Documentation string
	Value         string
	Decorators    []Decorator
}

type UnionDefinition struct {
	RecordDefinition
	Branches map[int]string
}

func (ud UnionDefinition) Kind() string { return "union" }

type ServiceDefinition struct {
	Kind    string
	Methods map[string]MethodDefinition
}

type MethodDefinition struct {
	Documentation string
	Decorators    []Decorator
	Type          string
	RequestType   string
	ResponseType  string
	Id            int
}

type ConstantDefinition struct {
	Type  BaseType
	Value string
}

type Field struct {
	Documentation string
	Decorators    []Decorator
	Type          FieldType
	Index         int
}

type IndentedStringBuilder struct {
	spaces int
	buffer bytes.Buffer
}

func NewIndentedStringBuilder(spaces int) *IndentedStringBuilder {
	return &IndentedStringBuilder{
		spaces: spaces,
	}
}

func (b *IndentedStringBuilder) AppendLine(texts ...string) *IndentedStringBuilder {
	text := ""
	if len(texts) > 0 {
		text = texts[0]
	}
	lines := getLines(text)
	for _, line := range lines {
		b.buffer.WriteString(strings.Repeat(" ", b.spaces) + strings.TrimRight(line, " \t") + "\n")
	}
	return b
}

func (b *IndentedStringBuilder) Append(text string) *IndentedStringBuilder {
	lines := getLines(text)
	for _, line := range lines {
		b.buffer.WriteString(strings.Repeat(" ", b.spaces) + strings.TrimRight(line, " \t"))
	}
	return b
}

func (b *IndentedStringBuilder) AppendMid(text string) *IndentedStringBuilder {
	if len(getLines(text)) > 1 {
		panic("AppendMid must not contain multiple lines")
	}
	b.buffer.WriteString(text)
	return b
}

func (b *IndentedStringBuilder) AppendEnd(text string) *IndentedStringBuilder {
	if len(getLines(text)) > 1 {
		panic("AppendEnd must not contain multiple lines")
	}
	b.buffer.WriteString(strings.TrimRight(text, " \t") + "\n")
	return b
}

func (b *IndentedStringBuilder) Indent(addSpaces int) *IndentedStringBuilder {
	b.spaces = max(0, b.spaces+addSpaces)
	return b
}

func (b *IndentedStringBuilder) Dedent(removeSpaces int) *IndentedStringBuilder {
	b.spaces = max(0, b.spaces-removeSpaces)
	return b
}

func (b *IndentedStringBuilder) CodeBlock(openingLine string, spaces int, fn func(b *IndentedStringBuilder), open string, close string) *IndentedStringBuilder {
	if openingLine != "" {
		b.Append(openingLine)
		b.AppendEnd(" " + open)
	} else {
		b.AppendLine(open)
	}
	b.Indent(spaces)
	fn(b)
	b.Dedent(spaces)
	b.AppendLine(close)
	return b
}

func (b *IndentedStringBuilder) String() string {
	return b.buffer.String()
}

func getLines(text string) []string {
	return strings.Split(text, "\n")
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}
