(function() {
    'use strict';

    const TextEventParser = {
        parseText: function(text) {
            const lines = text.split('\n').map(line => line.trim()).filter(line => line.length > 0);
            const events = [];
            let currentEvent = null;
            let currentEventLines = [];

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                const lineRole = this.analyzeLineRole(line);

                if (lineRole.type === 'DATE') {
                    if (currentEvent) {
                        this.finalizeEvent(currentEvent, currentEventLines);
                        events.push(currentEvent);
                    }
                    currentEvent = this.createEventFromDateLine(line);
                    currentEventLines = [];
                } else if (currentEvent) {
                    currentEventLines.push({ line: line, role: lineRole });
                }
            }

            if (currentEvent) {
                this.finalizeEvent(currentEvent, currentEventLines);
                events.push(currentEvent);
            }

            return events;
        },

        analyzeLineRole: function(line) {
            const roles = [];

            const dateScore = this.scoreAsDate(line);
            if (dateScore > 0) {
                roles.push({ type: 'DATE', score: dateScore });
            }

            const timeScore = this.scoreAsTime(line);
            if (timeScore > 0) {
                roles.push({ type: 'TIME', score: timeScore });
            }

            const locationScore = this.scoreAsLocation(line);
            if (locationScore > 0) {
                roles.push({ type: 'LOCATION', score: locationScore });
            }

            const titleScore = this.scoreAsTitle(line);
            roles.push({ type: 'TITLE', score: titleScore });

            const descriptionScore = this.scoreAsDescription(line);
            roles.push({ type: 'DESCRIPTION', score: descriptionScore });

            roles.sort((a, b) => b.score - a.score);
            return roles.length > 0 ? roles[0] : { type: 'DESCRIPTION', score: 0 };
        },

        scoreAsDate: function(line) {
            const datePatterns = [
                /\d{1,2}\.\d{1,2}(\.\d{2,4})?/,
                /\d{1,2}\/\d{1,2}(\/\d{2,4})?/,
                /\d{1,2}-\d{1,2}(-\d{2,4})?/
            ];

            for (const pattern of datePatterns) {
                if (pattern.test(line)) {
                    const dayNamePattern = /יום\s+[א-ת]/;
                    if (dayNamePattern.test(line)) {
                        return 100;
                    }
                    if (line.match(/^\d{1,2}[\.\/\-]\d{1,2}/)) {
                        return 90;
                    }
                    return 50;
                }
            }
            return 0;
        },

        scoreAsTime: function(line) {
            const timePatterns = [
                /^\d{1,2}[\.:]\d{2}\s*-\s*\d{1,2}[\.:]\d{2}$/,
                /^\d{1,2}\s*-\s*\d{1,2}$/,
                /^\d{1,2}[\.:]\d{2}$/
            ];

            for (const pattern of timePatterns) {
                if (pattern.test(line)) {
                    if (line.includes('-')) {
                        return 100;
                    }
                    return 50;
                }
            }
            return 0;
        },

        scoreAsLocation: function(line) {
            const locationKeywords = [
                'במרכז', 'מרכז', 'באולם', 'אולם', 'במתחם', 'מתחם',
                'בכרמים', 'כרמים', 'בבית', 'בית', 'במקום', 'מקום',
                'עם קבוצת', 'קבוצת', 'בקבוצה', 'קבוצה',
                'מתקיים ב', 'מתקיים ב-', 'מתקיים במ', 'מתקיים במ-',
                'ב-', 'ב ', 'במ-', 'במ '
            ];

            const lowerLine = line.toLowerCase();
            for (const keyword of locationKeywords) {
                if (lowerLine.includes(keyword.toLowerCase())) {
                    return 80;
                }
            }

            if (/^ב[א-ת]/.test(line) || /^במ[א-ת]/.test(line)) {
                return 60;
            }

            return 0;
        },

        scoreAsTitle: function(line) {
            if (this.scoreAsDate(line) > 0 || this.scoreAsTime(line) > 0 || this.scoreAsLocation(line) > 0) {
                return 0;
            }

            if (line.length < 3) {
                return 0;
            }

            if (line.length > 100) {
                return 30;
            }

            if (line.length > 20 && line.length < 80) {
                return 70;
            }

            return 50;
        },

        scoreAsDescription: function(line) {
            if (this.scoreAsDate(line) > 0 || this.scoreAsTime(line) > 0) {
                return 0;
            }

            if (this.scoreAsLocation(line) > 0) {
                return 20;
            }

            return 30;
        },

        createEventFromDateLine: function(line) {
            const dateMatch = line.match(/(\d{1,2})[\.\/\-](\d{1,2})(?:[\.\/\-](\d{2,4}))?/);
            if (!dateMatch) {
                return null;
            }

            let day = parseInt(dateMatch[1], 10);
            let month = parseInt(dateMatch[2], 10);
            let year = null;

            if (dateMatch[3]) {
                year = parseInt(dateMatch[3], 10);
                if (year < 100) {
                    year = 2000 + year;
                }
            }

            const currentDate = new Date();
            const currentYear = currentDate.getFullYear();
            const currentMonth = currentDate.getMonth() + 1;
            const currentDay = currentDate.getDate();

            if (day > 31 || month > 12) {
                const temp = day;
                day = month;
                month = temp;
            }

            if (!year) {
                year = currentYear;
                if (month < currentMonth || (month === currentMonth && day < currentDay)) {
                    if (month === 12 && currentMonth === 1) {
                        year = currentYear;
                    } else {
                        year = currentYear + 1;
                    }
                }
            }

            const eventDate = new Date(year, month - 1, day);

            if (eventDate.getMonth() !== month - 1 || eventDate.getDate() !== day) {
                return null;
            }

            const monthStr = String(month).padStart(2, '0');
            const dayStr = String(day).padStart(2, '0');

            return {
                date: `${year}-${monthStr}-${dayStr}`,
                startTime: '',
                endTime: '',
                title: '',
                location: '',
                description: ''
            };
        },

        finalizeEvent: function(event, lines) {
            let titleAssigned = false;
            let timeAssigned = false;
            const descriptionParts = [];
            const locationParts = [];

            const sortedLines = lines.slice().sort((a, b) => {
                if (a.role.type === 'TIME' && b.role.type !== 'TIME') return -1;
                if (a.role.type !== 'TIME' && b.role.type === 'TIME') return 1;
                if (a.role.type === 'TITLE' && b.role.type !== 'TITLE') return -1;
                if (a.role.type !== 'TITLE' && b.role.type === 'TITLE') return 1;
                return b.role.score - a.role.score;
            });

            for (const item of sortedLines) {
                const line = item.line;
                const role = item.role;

                if (role.type === 'TIME' && !timeAssigned) {
                    const timeRange = this.parseTimeRange(line);
                    if (timeRange) {
                        event.startTime = timeRange.start;
                        event.endTime = timeRange.end;
                        timeAssigned = true;
                        continue;
                    }
                }

                if (role.type === 'LOCATION') {
                    const cleanLocation = this.cleanLocationLine(line);
                    if (cleanLocation) {
                        locationParts.push(cleanLocation);
                        continue;
                    }
                }

                if (role.type === 'TITLE' && !titleAssigned) {
                    event.title = line;
                    titleAssigned = true;
                    continue;
                }

                if (role.type !== 'DATE' && role.type !== 'TIME') {
                    descriptionParts.push(line);
                }
            }

            if (!titleAssigned && descriptionParts.length > 0) {
                event.title = descriptionParts.shift();
            }

            if (locationParts.length > 0) {
                event.location = locationParts.join(', ');
            }

            if (descriptionParts.length > 0) {
                event.description = descriptionParts.join('\n');
            }

            if (!event.title) {
                event.title = 'אירוע';
            }
        },

        cleanLocationLine: function(line) {
            let cleaned = line.trim();

            const prefixes = [
                'מתקיים ב', 'מתקיים ב-', 'מתקיים במ', 'מתקיים במ-',
                'ב-', 'במ-'
            ];

            for (const prefix of prefixes) {
                if (cleaned.toLowerCase().startsWith(prefix.toLowerCase())) {
                    cleaned = cleaned.substring(prefix.length).trim();
                    break;
                }
            }

            if (cleaned.length < 2) {
                return null;
            }

            return cleaned;
        },

        parseTimeRange: function(line) {
            const cleanLine = line.replace(/\s/g, '');
            const patterns = [
                { regex: /(\d{1,2})[\.:](\d{2})-(\d{1,2})[\.:](\d{2})/, hasMinutes: true },
                { regex: /(\d{1,2})-(\d{1,2})/, hasMinutes: false }
            ];

            for (const pattern of patterns) {
                const match = cleanLine.match(pattern.regex);
                if (match) {
                    if (pattern.hasMinutes && match.length >= 5) {
                        const startHour = parseInt(match[1], 10);
                        const startMin = parseInt(match[2], 10);
                        const endHour = parseInt(match[3], 10);
                        const endMin = parseInt(match[4], 10);
                        if (startHour >= 0 && startHour <= 23 && endHour >= 0 && endHour <= 23 &&
                            startMin >= 0 && startMin <= 59 && endMin >= 0 && endMin <= 59) {
                            return {
                                start: `${String(startHour).padStart(2, '0')}:${String(startMin).padStart(2, '0')}`,
                                end: `${String(endHour).padStart(2, '0')}:${String(endMin).padStart(2, '0')}`
                            };
                        }
                    } else if (!pattern.hasMinutes && match.length >= 3) {
                        const startHour = parseInt(match[1], 10);
                        const endHour = parseInt(match[2], 10);
                        if (startHour >= 0 && startHour <= 23 && endHour >= 0 && endHour <= 23) {
                            return {
                                start: `${String(startHour).padStart(2, '0')}:00`,
                                end: `${String(endHour).padStart(2, '0')}:00`
                            };
                        }
                    }
                }
            }
            return null;
        }
    };

    window.TextEventParser = TextEventParser;
})();
