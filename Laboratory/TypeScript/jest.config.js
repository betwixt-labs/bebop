module.exports = {
    verbose: true,
    testMatch: ["**/test/*.ts"],
    testPathIgnorePatterns: [".*BebopView.ts"],
    transform: {"^.+\\.(ts|tsx)$": "ts-jest"},
};
