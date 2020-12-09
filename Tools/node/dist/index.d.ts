export interface Issue {
    severity: string;
    startLine: number;
    startColumn: number;
    endLine: number;
    endColumn: number;
    description: string;
}
/** Validate schema file passed by path. */
export declare function check(path: string): Promise<{
    error: boolean;
    issues?: Issue[];
}>;
/** Validate schema passed as string. */
export declare function checkText(contents: string): Promise<{
    error: boolean;
    issues?: Issue[];
}>;
