import { CompilerContext, IndentedStringBuilder, commitResult, readContext } from "./kernel";

export function chordCompile() {
  const input = readContext();

  commitResult(generateDocumentation(input));
}

function generateDocumentation(context: CompilerContext): string {
  const builder = new IndentedStringBuilder();

  builder.appendLine(`Compiler Context Documentation`);
  builder.appendLine(`===============================`);
  builder.appendLine();

  // Generate constants table
  builder.appendLine(`Constants:`);
  builder.appendLine(`+--------+--------+-------+`);
  builder.appendLine(`| Name   | Type   | Value |`);
  builder.appendLine(`+--------+--------+-------+`);
  for (const [name, constant] of Object.entries(context.constants)) {
    builder.appendLine(`| ${pad(name, 6)} | ${pad(constant.type, 6)} | ${pad(constant.value, 5)} |`);
  }
  builder.appendLine(`+--------+--------+-------+`);
  builder.appendLine();

  // Generate definitions table
  builder.appendLine(`Definitions:`);
  builder.appendLine(`+-------------+----------+`);
  builder.appendLine(`| Name        | Kind     |`);
  builder.appendLine(`+-------------+----------+`);
  for (const [name, definition] of Object.entries(context.definitions)) {
    builder.appendLine(`| ${pad(name, 11)} | ${pad(definition.kind, 8)} |`);
  }
  builder.appendLine(`+-------------+----------+`);
  builder.appendLine();

  // Generate fields table for each struct definition
  for (const [name, definition] of Object.entries(context.definitions)) {
    if (definition.kind === "struct") {
      builder.appendLine(`Fields for struct "${name}":`);
      builder.appendLine(`+-------------+----------+`);
      builder.appendLine(`| Name        | Type     |`);
      builder.appendLine(`+-------------+----------+`);
      for (const [fieldName, field] of Object.entries(definition.fields)) {
        builder.appendLine(`| ${pad(fieldName, 11)} | ${pad(field.type, 8)} |`);
      }
      builder.appendLine(`+-------------+----------+`);
      builder.appendLine();
    }
  }

  // Generate services table
  builder.appendLine(`Services:`);
  builder.appendLine(`+-------------+`);
  builder.appendLine(`| Name        |`);
  builder.appendLine(`+-------------+`);
  for (const [name, service] of Object.entries(context.services)) {
    builder.appendLine(`| ${pad(name, 11)} |`);
  }
  builder.appendLine(`+-------------+`);
  builder.appendLine();

  // Generate methods table for each service
  for (const [serviceName, service] of Object.entries(context.services)) {
    builder.appendLine(`Methods for service "${serviceName}":`);
    builder.appendLine(`+----------------+-------------+-------------+`);
    builder.appendLine(`| Name           | Request     | Response    |`);
    builder.appendLine(`+----------------+-------------+-------------+`);
    for (const [methodName, method] of Object.entries(service.methods)) {
      builder.appendLine(`| ${pad(methodName, 14)} | ${pad(method.requestType, 11)} | ${pad(method.responseType, 11)} |`);
    }
    builder.appendLine(`+----------------+-------------+-------------+`);
    builder.appendLine();
  }

  return builder.toString();
}

function pad(str: string, length: number): string {
  return str.padEnd(length, " ");
}