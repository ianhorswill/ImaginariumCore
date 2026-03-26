{

const COMMA = {
    scope: 'keyword',
    begin: ','
};

const PERIOD = {
    scope: 'keyword',
    begin: '\\.'
};

hljs.registerLanguage('imaginarium', function () {
    return {
        keywords: {
            keyword: ['and', 'or', 'not',
                'can', 'must', 'should',
                'are', 'is', 'be',
                'kind', 'way',
                'of', 'from',
                'a', 'A', 'An', 'an',
                '.',
                'has', 'have',
                'mutually', 'exclusive',
                'other',
                'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten',
            ]
        },
        contains: [ COMMA, PERIOD ]
    }
})
}